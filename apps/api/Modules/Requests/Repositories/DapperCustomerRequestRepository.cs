using Dapper;
using LineCom.Api.Infrastructure.Database;

namespace LineCom.Api.Modules.Requests.Repositories;

public sealed class DapperCustomerRequestRepository : ICustomerRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperCustomerRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CreatedCustomerRequestRecord> CreateAsync(
        CustomerRequestDraft draft,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var requestedAt = DateTimeOffset.UtcNow;
            var year = requestedAt.Year;
            var sequence = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    RequestNumberSql.GetNextSequence,
                    new { Year = year },
                    transaction,
                    cancellationToken: cancellationToken));
            var number = $"ЗК{year % 100:00}-{sequence:0000}";

            var organization = await connection.QuerySingleOrDefaultAsync<RequestOrganizationSnapshot>(
                new CommandDefinition(
                    CustomerRequestSql.FindOrganizationSnapshot,
                    new { UserId = draft.User.Id },
                    transaction,
                    cancellationToken: cancellationToken));

            var productIds = draft.Items.Select(item => item.ProductId).Distinct().ToArray();
            var productRows = (await connection.QueryAsync<RequestProductSnapshot>(
                new CommandDefinition(
                    CustomerRequestSql.FindProductSnapshots,
                    new { ProductIds = productIds },
                    transaction,
                    cancellationToken: cancellationToken))).ToDictionary(
                        product => product.ProductId);

            if (productRows.Count != productIds.Length)
            {
                throw new ProductNotAvailableException();
            }

            var created = await connection.QuerySingleAsync<InsertedRequestRecord>(
                new CommandDefinition(
                    CustomerRequestSql.InsertRequest,
                    new
                    {
                        Number = number,
                        NumberYear = year,
                        NumberSequence = sequence,
                        UserId = draft.User.Id,
                        OrganizationId = organization?.Id,
                        Source = draft.Source,
                        CustomerName = draft.User.Name,
                        CustomerEmail = draft.User.Email,
                        CustomerPhone = draft.User.Phone,
                        OrganizationName = organization?.Name,
                        OrganizationInn = organization?.Inn,
                        OrganizationContactPerson = organization?.ContactPerson,
                        OrganizationPhone = organization?.Phone,
                        OrganizationEmail = organization?.Email,
                        CustomerComment = draft.CustomerComment
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            var createdItems = new List<CreatedCustomerRequestItemRecord>(draft.Items.Count);
            for (var index = 0; index < draft.Items.Count; index++)
            {
                var item = draft.Items[index];
                var product = productRows[item.ProductId];

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        CustomerRequestSql.InsertRequestItem,
                        new
                        {
                            RequestId = created.Id,
                            product.ProductId,
                            item.Quantity,
                            product.ProductName,
                            product.ProductSlug,
                            product.ProductSku,
                            product.CategoryName,
                            product.CategorySlug,
                            product.BrandName,
                            product.BrandSlug,
                            product.AvailabilityStatus,
                            product.SaleUnit,
                            product.UnitQuantity,
                            item.CustomerComment,
                            SortOrder = index
                        },
                        transaction,
                        cancellationToken: cancellationToken));

                createdItems.Add(new CreatedCustomerRequestItemRecord(
                    product.ProductId,
                    product.ProductName,
                    product.ProductSku,
                    product.SaleUnit,
                    product.UnitQuantity,
                    item.Quantity,
                    item.CustomerComment));
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    CustomerRequestSql.InsertCreatedHistory,
                    new
                    {
                        RequestId = created.Id,
                        ActorUserId = draft.User.Id
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return new CreatedCustomerRequestRecord(
                created.Number,
                created.Status,
                created.Source,
                created.CustomerComment,
                created.CreatedAt,
                createdItems);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CustomerRequestListRecordResponse> GetRequestsAsync(
        CustomerRequestReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var parameters = new
        {
            query.UserId,
            query.Status,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        };
        var totalItems = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                CustomerRequestSql.CountCurrentUserRequests,
                parameters,
                cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<CustomerRequestListRecord>(
            new CommandDefinition(
                CustomerRequestSql.FindCurrentUserRequests,
                parameters,
                cancellationToken: cancellationToken))).ToArray();

        return new CustomerRequestListRecordResponse(items, totalItems);
    }

    public async Task<CustomerRequestDetailRecord?> GetRequestAsync(
        CustomerRequestReadDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var request = await connection.QuerySingleOrDefaultAsync<RequestDetailRow>(
            new CommandDefinition(
                CustomerRequestSql.FindCurrentUserRequestDetail,
                new
                {
                    query.UserId,
                    query.Number
                },
                cancellationToken: cancellationToken));
        if (request is null)
        {
            return null;
        }

        var items = (await connection.QueryAsync<CreatedCustomerRequestItemRecord>(
            new CommandDefinition(
                CustomerRequestSql.FindRequestItems,
                new { RequestId = request.Id },
                cancellationToken: cancellationToken))).ToArray();
        var history = (await connection.QueryAsync<CustomerRequestHistoryRecord>(
            new CommandDefinition(
                CustomerRequestSql.FindRequestHistory,
                new { RequestId = request.Id },
                cancellationToken: cancellationToken))).ToArray();

        return new CustomerRequestDetailRecord(
            request.Number,
            request.Status,
            request.Source,
            new RequestCustomerSnapshotRecord(
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone),
            request.OrganizationName is null
                ? null
                : new RequestOrganizationSnapshotRecord(
                    request.OrganizationName,
                    request.OrganizationInn,
                    request.OrganizationContactPerson),
            request.CustomerComment,
            request.CreatedAt,
            items,
            history);
    }

    private sealed record RequestOrganizationSnapshot(
        Guid Id,
        string Name,
        string? Inn,
        string? ContactPerson,
        string? Phone,
        string? Email);

    private sealed record RequestProductSnapshot(
        Guid ProductId,
        string ProductName,
        string ProductSlug,
        string? ProductSku,
        string CategoryName,
        string CategorySlug,
        string? BrandName,
        string? BrandSlug,
        string AvailabilityStatus,
        string SaleUnit,
        string UnitQuantity);

    private sealed record InsertedRequestRecord(
        Guid Id,
        string Number,
        string Status,
        string Source,
        string? CustomerComment,
        DateTimeOffset CreatedAt);

    private sealed record RequestDetailRow(
        Guid Id,
        string Number,
        string Status,
        string Source,
        string CustomerName,
        string? CustomerEmail,
        string? CustomerPhone,
        string? OrganizationName,
        string? OrganizationInn,
        string? OrganizationContactPerson,
        string? CustomerComment,
        DateTimeOffset CreatedAt);
}
