using Dapper;
using LineCom.Api.Infrastructure.Database;

namespace LineCom.Api.Modules.Requests.Repositories;

public sealed class DapperAdminRequestRepository : IAdminRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminRequestListRecordResponse> GetRequestsAsync(
        AdminRequestReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var parameters = new
        {
            query.Status,
            query.Number,
            query.Contact,
            query.Organization,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        };
        var totalItems = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminRequestSql.CountRequests,
                parameters,
                cancellationToken: cancellationToken));
        var rows = (await connection.QueryAsync<AdminRequestListRow>(
            new CommandDefinition(
                AdminRequestSql.FindRequests,
                parameters,
                cancellationToken: cancellationToken))).ToArray();
        var items = rows
            .Select(row => new AdminRequestListRecord(
                row.Number,
                row.Status,
                row.Source,
                row.ItemsCount,
                new RequestCustomerSnapshotRecord(
                    row.CustomerName,
                    row.CustomerEmail,
                    row.CustomerPhone),
                row.OrganizationName is null
                    ? null
                    : new RequestOrganizationSnapshotRecord(
                        row.OrganizationName,
                        row.OrganizationInn,
                        row.OrganizationContactPerson),
                row.CustomerComment,
                row.InternalComment,
                ToUtcDateTimeOffset(row.CreatedAt),
                ToUtcDateTimeOffset(row.UpdatedAt)))
            .ToArray();

        return new AdminRequestListRecordResponse(items, totalItems);
    }

    public async Task<AdminRequestDetailRecord?> GetRequestAsync(
        string number,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var request = await connection.QuerySingleOrDefaultAsync<AdminRequestDetailRow>(
            new CommandDefinition(
                AdminRequestSql.FindRequestDetail,
                new { Number = number },
                cancellationToken: cancellationToken));
        if (request is null)
        {
            return null;
        }

        var items = (await connection.QueryAsync<CreatedCustomerRequestItemRecord>(
            new CommandDefinition(
                AdminRequestSql.FindRequestItems,
                new { RequestId = request.Id },
                cancellationToken: cancellationToken))).ToArray();
        var historyRows = (await connection.QueryAsync<AdminRequestHistoryRow>(
            new CommandDefinition(
                AdminRequestSql.FindRequestHistory,
                new { RequestId = request.Id },
                cancellationToken: cancellationToken))).ToArray();
        var history = historyRows
            .Select(row => new CustomerRequestHistoryRecord(
                row.Event,
                row.Message,
                ToUtcDateTimeOffset(row.CreatedAt)))
            .ToArray();

        return new AdminRequestDetailRecord(
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
            request.InternalComment,
            ToUtcDateTimeOffset(request.CreatedAt),
            ToUtcDateTimeOffset(request.UpdatedAt),
            items,
            history);
    }

    public async Task<AdminRequestDetailRecord?> UpdateStatusAsync(
        AdminRequestStatusUpdate update,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var current = await connection.QuerySingleOrDefaultAsync<AdminRequestForUpdateRow>(
                new CommandDefinition(
                    AdminRequestSql.FindRequestForUpdate,
                    new { update.Number },
                    transaction,
                    cancellationToken: cancellationToken));
            if (current is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(current.Status, update.Status, StringComparison.Ordinal))
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        AdminRequestSql.UpdateStatus,
                        new
                        {
                            RequestId = current.Id,
                            update.Status
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        AdminRequestSql.InsertStatusChangedHistory,
                        new
                        {
                            RequestId = current.Id,
                            update.ActorUserId,
                            OldStatus = current.Status,
                            NewStatus = update.Status
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetRequestAsync(update.Number, cancellationToken);
    }

    public async Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(
        AdminRequestInternalCommentUpdate update,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var current = await connection.QuerySingleOrDefaultAsync<AdminRequestForUpdateRow>(
                new CommandDefinition(
                    AdminRequestSql.FindRequestForUpdate,
                    new { update.Number },
                    transaction,
                    cancellationToken: cancellationToken));
            if (current is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(current.InternalComment, update.InternalComment, StringComparison.Ordinal))
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        AdminRequestSql.UpdateInternalComment,
                        new
                        {
                            RequestId = current.Id,
                            update.InternalComment
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        AdminRequestSql.InsertInternalCommentHistory,
                        new
                        {
                            RequestId = current.Id,
                            update.ActorUserId,
                            update.InternalComment
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetRequestAsync(update.Number, cancellationToken);
    }

    private static DateTimeOffset ToUtcDateTimeOffset(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utcValue);
    }

    private sealed record AdminRequestListRow(
        string Number,
        string Status,
        string Source,
        int ItemsCount,
        string CustomerName,
        string? CustomerEmail,
        string? CustomerPhone,
        string? OrganizationName,
        string? OrganizationInn,
        string? OrganizationContactPerson,
        string? CustomerComment,
        string? InternalComment,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record AdminRequestDetailRow(
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
        string? InternalComment,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record AdminRequestForUpdateRow(
        Guid Id,
        string Status,
        string? InternalComment);

    private sealed record AdminRequestHistoryRow(
        string Event,
        string Message,
        DateTime CreatedAt);
}
