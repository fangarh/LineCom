namespace LineCom.Api.Modules.Requests.DTOs;

public sealed record AdminRequestListQuery(
    int? Page,
    int? PageSize,
    string? Status,
    string? Number,
    string? Contact,
    string? Organization);

public sealed record AdminRequestListResponse(
    IReadOnlyList<AdminRequestListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminRequestListItemDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    int ItemsCount,
    RequestCustomerSnapshotDto Customer,
    RequestOrganizationSnapshotDto? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminRequestDetailDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    RequestCustomerSnapshotDto Customer,
    RequestOrganizationSnapshotDto? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CustomerRequestItemDto> Items,
    IReadOnlyList<CustomerRequestHistoryDto> History);

public sealed record UpdateAdminRequestStatusCommand(string? Status);

public sealed record UpdateAdminRequestInternalCommentCommand(string? InternalComment);
