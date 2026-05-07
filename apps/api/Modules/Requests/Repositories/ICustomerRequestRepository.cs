using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Requests.Repositories;

public sealed record CustomerRequestDraft(
    CurrentUserDto User,
    string Source,
    string? CustomerComment,
    IReadOnlyList<CustomerRequestDraftItem> Items);

public sealed record CustomerRequestDraftItem(
    Guid ProductId,
    int Quantity,
    string? CustomerComment);

public sealed record CreatedCustomerRequestRecord(
    string Number,
    string Status,
    string Source,
    string? CustomerComment,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CreatedCustomerRequestItemRecord> Items);

public sealed record CreatedCustomerRequestItemRecord(
    Guid ProductId,
    string ProductName,
    string? ProductSku,
    string SaleUnit,
    string UnitQuantity,
    int Quantity,
    string? CustomerComment);

public sealed record CustomerRequestReadListQuery(
    Guid UserId,
    int Page,
    int PageSize,
    string? Status);

public sealed record CustomerRequestListRecordResponse(
    IReadOnlyList<CustomerRequestListRecord> Items,
    int TotalItems);

public sealed record CustomerRequestListRecord(
    string Number,
    string Status,
    string Source,
    int ItemsCount,
    string? CustomerComment,
    DateTimeOffset CreatedAt);

public sealed record CustomerRequestReadDetailQuery(
    Guid UserId,
    string Number);

public sealed record CustomerRequestDetailRecord(
    string Number,
    string Status,
    string Source,
    RequestCustomerSnapshotRecord Customer,
    RequestOrganizationSnapshotRecord? Organization,
    string? CustomerComment,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CreatedCustomerRequestItemRecord> Items,
    IReadOnlyList<CustomerRequestHistoryRecord> History);

public sealed record RequestCustomerSnapshotRecord(
    string Name,
    string? Email,
    string? Phone);

public sealed record RequestOrganizationSnapshotRecord(
    string Name,
    string? Inn,
    string? ContactPerson);

public sealed record CustomerRequestHistoryRecord(
    string Event,
    string Message,
    DateTimeOffset CreatedAt);

public sealed class ProductNotAvailableException : Exception
{
}

public interface ICustomerRequestRepository
{
    Task<CreatedCustomerRequestRecord> CreateAsync(
        CustomerRequestDraft draft,
        CancellationToken cancellationToken = default);

    Task<CustomerRequestListRecordResponse> GetRequestsAsync(
        CustomerRequestReadListQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerRequestDetailRecord?> GetRequestAsync(
        CustomerRequestReadDetailQuery query,
        CancellationToken cancellationToken = default);
}
