namespace LineCom.Api.Modules.Requests.Repositories;

public sealed record AdminRequestReadListQuery(
    int Page,
    int PageSize,
    string? Status,
    string? Number,
    string? Contact,
    string? Organization);

public sealed record AdminRequestListRecordResponse(
    IReadOnlyList<AdminRequestListRecord> Items,
    int TotalItems);

public sealed record AdminRequestListRecord(
    string Number,
    string Status,
    string Source,
    int ItemsCount,
    RequestCustomerSnapshotRecord Customer,
    RequestOrganizationSnapshotRecord? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminRequestDetailRecord(
    string Number,
    string Status,
    string Source,
    RequestCustomerSnapshotRecord Customer,
    RequestOrganizationSnapshotRecord? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CreatedCustomerRequestItemRecord> Items,
    IReadOnlyList<CustomerRequestHistoryRecord> History);

public sealed record AdminRequestStatusUpdate(
    string Number,
    string Status,
    Guid ActorUserId);

public sealed record AdminRequestInternalCommentUpdate(
    string Number,
    string? InternalComment,
    Guid ActorUserId);

public interface IAdminRequestRepository
{
    Task<AdminRequestListRecordResponse> GetRequestsAsync(
        AdminRequestReadListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailRecord?> GetRequestAsync(
        string number,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailRecord?> UpdateStatusAsync(
        AdminRequestStatusUpdate update,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(
        AdminRequestInternalCommentUpdate update,
        CancellationToken cancellationToken = default);
}
