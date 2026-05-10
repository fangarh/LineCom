using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Modules.Requests.Services;

public sealed class AdminRequestService : IAdminRequestService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 60;

    private readonly IAuthCurrentUserService _currentUserService;
    private readonly IAdminRequestRepository _repository;
    private readonly IRequestReferenceData _requestReferenceData;
    private readonly IPublicCatalogReferenceData _catalogReferenceData;

    public AdminRequestService(
        IAuthCurrentUserService currentUserService,
        IAdminRequestRepository repository,
        IRequestReferenceData requestReferenceData,
        IPublicCatalogReferenceData catalogReferenceData)
    {
        _currentUserService = currentUserService;
        _repository = repository;
        _requestReferenceData = requestReferenceData;
        _catalogReferenceData = catalogReferenceData;
    }

    public async Task<AdminRequestListResponse> GetRequestsAsync(
        HttpContext httpContext,
        AdminRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        await RequireStaffAsync(httpContext, cancellationToken);
        var status = NormalizeText(query.Status);
        if (status is not null)
        {
            _requestReferenceData.GetStatus(status);
        }

        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);
        var result = await _repository.GetRequestsAsync(
            new AdminRequestReadListQuery(
                page,
                pageSize,
                status,
                NormalizeText(query.Number),
                NormalizeText(query.Contact),
                NormalizeText(query.Organization)),
            cancellationToken);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)pageSize);
        var items = result.Items.Select(ToListDto).ToArray();

        return new AdminRequestListResponse(items, page, pageSize, result.TotalItems, totalPages);
    }

    public async Task<AdminRequestDetailDto> GetRequestAsync(
        HttpContext httpContext,
        string number,
        CancellationToken cancellationToken = default)
    {
        await RequireStaffAsync(httpContext, cancellationToken);
        var normalizedNumber = NormalizeText(number);
        if (normalizedNumber is null)
        {
            throw RequestErrors.NotFound();
        }

        var record = await _repository.GetRequestAsync(normalizedNumber, cancellationToken);
        if (record is null)
        {
            throw RequestErrors.NotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminRequestDetailDto> UpdateStatusAsync(
        HttpContext httpContext,
        string number,
        UpdateAdminRequestStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireStaffAsync(httpContext, cancellationToken);
        var normalizedNumber = NormalizeText(number);
        var status = NormalizeText(command.Status);
        if (normalizedNumber is null || status is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        _requestReferenceData.GetStatus(status);

        var record = await _repository.UpdateStatusAsync(
            new AdminRequestStatusUpdate(normalizedNumber, status, actor.Id),
            cancellationToken);
        if (record is null)
        {
            throw RequestErrors.NotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminRequestDetailDto> UpdateInternalCommentAsync(
        HttpContext httpContext,
        string number,
        UpdateAdminRequestInternalCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireStaffAsync(httpContext, cancellationToken);
        var normalizedNumber = NormalizeText(number);
        if (normalizedNumber is null)
        {
            throw RequestErrors.NotFound();
        }

        var record = await _repository.UpdateInternalCommentAsync(
            new AdminRequestInternalCommentUpdate(
                normalizedNumber,
                NormalizeText(command.InternalComment),
                actor.Id),
            cancellationToken);
        if (record is null)
        {
            throw RequestErrors.NotFound();
        }

        return ToDetailDto(record);
    }

    private async Task<CurrentUserDto> RequireStaffAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var session = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        if (session.User.Role is "seller" or "admin")
        {
            return session.User;
        }

        throw AuthErrors.Forbidden();
    }

    private AdminRequestListItemDto ToListDto(AdminRequestListRecord record)
    {
        return new AdminRequestListItemDto(
            record.Number,
            _requestReferenceData.GetStatus(record.Status),
            record.Source,
            record.ItemsCount,
            new RequestCustomerSnapshotDto(
                record.Customer.Name,
                record.Customer.Email,
                record.Customer.Phone),
            record.Organization is null
                ? null
                : new RequestOrganizationSnapshotDto(
                    record.Organization.Name,
                    record.Organization.Inn,
                    record.Organization.ContactPerson),
            record.CustomerComment,
            record.InternalComment,
            record.CreatedAt,
            record.UpdatedAt);
    }

    private AdminRequestDetailDto ToDetailDto(AdminRequestDetailRecord record)
    {
        return new AdminRequestDetailDto(
            record.Number,
            _requestReferenceData.GetStatus(record.Status),
            record.Source,
            new RequestCustomerSnapshotDto(
                record.Customer.Name,
                record.Customer.Email,
                record.Customer.Phone),
            record.Organization is null
                ? null
                : new RequestOrganizationSnapshotDto(
                    record.Organization.Name,
                    record.Organization.Inn,
                    record.Organization.ContactPerson),
            record.CustomerComment,
            record.InternalComment,
            record.CreatedAt,
            record.UpdatedAt,
            record.Items
                .Select(item => new CustomerRequestItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.ProductSku,
                    _catalogReferenceData.GetSaleUnit(item.SaleUnit),
                    item.UnitQuantity,
                    item.Quantity,
                    item.CustomerComment))
                .ToArray(),
            record.History
                .Select(history => new CustomerRequestHistoryDto(
                    history.Event,
                    history.Message,
                    history.CreatedAt))
                .ToArray());
    }

    private static int NormalizePage(int? value)
    {
        return value is null or < 1 ? DefaultPage : value.Value;
    }

    private static int NormalizePageSize(int? value)
    {
        return value is null or < 1 ? DefaultPageSize : Math.Min(value.Value, MaxPageSize);
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
