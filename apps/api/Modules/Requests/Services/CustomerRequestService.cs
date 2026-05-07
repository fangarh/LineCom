using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Modules.Requests.Services;

public sealed class CustomerRequestService : ICustomerRequestService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 60;

    private static readonly IReadOnlySet<string> AllowedSources = new[]
    {
        "cart",
        "quick_order"
    }.ToHashSet(StringComparer.Ordinal);

    private readonly IAuthCurrentUserService _currentUserService;
    private readonly ICustomerRequestRepository _requestRepository;
    private readonly IRequestReferenceData _requestReferenceData;
    private readonly IPublicCatalogReferenceData _catalogReferenceData;

    public CustomerRequestService(
        IAuthCurrentUserService currentUserService,
        ICustomerRequestRepository requestRepository,
        IRequestReferenceData requestReferenceData,
        IPublicCatalogReferenceData catalogReferenceData)
    {
        _currentUserService = currentUserService;
        _requestRepository = requestRepository;
        _requestReferenceData = requestReferenceData;
        _catalogReferenceData = catalogReferenceData;
    }

    public async Task<CustomerRequestDetailDto> CreateRequestAsync(
        HttpContext httpContext,
        CreateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var source = NormalizeText(command.Source);
        if (source is null || !AllowedSources.Contains(source))
        {
            throw AuthErrors.InvalidRequest();
        }

        if (currentSession.User.Email is null && currentSession.User.Phone is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        var items = NormalizeItems(command.Items);
        var draft = new CustomerRequestDraft(
            currentSession.User,
            source,
            NormalizeText(command.CustomerComment),
            items);

        try
        {
            var created = await _requestRepository.CreateAsync(draft, cancellationToken);

            return ToDto(created);
        }
        catch (ProductNotAvailableException)
        {
            throw RequestErrors.ProductNotAvailable();
        }
    }

    public async Task<CustomerRequestListResponse> GetRequestsAsync(
        HttpContext httpContext,
        CustomerRequestListQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var status = NormalizeText(query.Status);
        if (status is not null)
        {
            _requestReferenceData.GetStatus(status);
        }

        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);
        var result = await _requestRepository.GetRequestsAsync(
            new CustomerRequestReadListQuery(
                currentSession.User.Id,
                page,
                pageSize,
                status),
            cancellationToken);

        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)pageSize);
        var items = result.Items
            .Select(item => new CustomerRequestListItemDto(
                item.Number,
                _requestReferenceData.GetStatus(item.Status),
                item.Source,
                item.ItemsCount,
                item.CustomerComment,
                item.CreatedAt))
            .ToArray();

        return new CustomerRequestListResponse(items, page, pageSize, result.TotalItems, totalPages);
    }

    public async Task<CustomerRequestDetailDto> GetRequestAsync(
        HttpContext httpContext,
        string number,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var normalizedNumber = NormalizeText(number);
        if (normalizedNumber is null)
        {
            throw RequestErrors.NotFound();
        }

        var record = await _requestRepository.GetRequestAsync(
            new CustomerRequestReadDetailQuery(currentSession.User.Id, normalizedNumber),
            cancellationToken);
        if (record is null)
        {
            throw RequestErrors.NotFound();
        }

        return ToDto(record);
    }

    private CustomerRequestDetailDto ToDto(CreatedCustomerRequestRecord created)
    {
        return new CustomerRequestDetailDto(
            created.Number,
            _requestReferenceData.GetStatus(created.Status),
            created.Source,
            created.CustomerComment,
            created.CreatedAt,
            created.Items
                .Select(item => new CustomerRequestItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.ProductSku,
                    _catalogReferenceData.GetSaleUnit(item.SaleUnit),
                    item.UnitQuantity,
                    item.Quantity,
                    item.CustomerComment))
                .ToArray());
    }

    private CustomerRequestDetailDto ToDto(CustomerRequestDetailRecord record)
    {
        return new CustomerRequestDetailDto(
            record.Number,
            _requestReferenceData.GetStatus(record.Status),
            record.Source,
            record.CustomerComment,
            record.CreatedAt,
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
        if (value is null or < 1)
        {
            return DefaultPageSize;
        }

        return Math.Min(value.Value, MaxPageSize);
    }

    private static IReadOnlyList<CustomerRequestDraftItem> NormalizeItems(
        IReadOnlyList<CreateRequestItemCommand>? items)
    {
        if (items is null || items.Count == 0)
        {
            throw RequestErrors.InvalidItems();
        }

        var normalizedItems = new List<CustomerRequestDraftItem>(items.Count);
        foreach (var item in items)
        {
            if (item.ProductId == Guid.Empty || item.Quantity < 1)
            {
                throw RequestErrors.InvalidItems();
            }

            normalizedItems.Add(new CustomerRequestDraftItem(
                item.ProductId,
                item.Quantity,
                NormalizeText(item.CustomerComment)));
        }

        return normalizedItems;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
