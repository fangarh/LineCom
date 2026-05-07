using System.Text.Json.Serialization;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Requests.DTOs;

public sealed record CreateRequestCommand(
    string? Source,
    string? CustomerComment,
    IReadOnlyList<CreateRequestItemCommand>? Items);

public sealed record CreateRequestItemCommand(
    Guid ProductId,
    int Quantity,
    string? CustomerComment);

public sealed record CustomerRequestListQuery(
    int? Page,
    int? PageSize,
    string? Status);

public sealed record CustomerRequestListResponse(
    IReadOnlyList<CustomerRequestListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record CustomerRequestListItemDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    int ItemsCount,
    string? CustomerComment,
    DateTimeOffset CreatedAt);

public sealed record CustomerRequestDetailDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    string? CustomerComment,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CustomerRequestItemDto> Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    RequestCustomerSnapshotDto? Customer = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    RequestOrganizationSnapshotDto? Organization = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<CustomerRequestHistoryDto>? History = null);

public sealed record RequestCustomerSnapshotDto(
    string Name,
    string? Email,
    string? Phone);

public sealed record RequestOrganizationSnapshotDto(
    string Name,
    string? Inn,
    string? ContactPerson);

public sealed record CustomerRequestItemDto(
    Guid ProductId,
    string ProductName,
    string? ProductSku,
    PublicCodeLabelDto SaleUnit,
    string UnitQuantity,
    int Quantity,
    string? CustomerComment);

public sealed record RequestStatusDto(
    string Code,
    string Label);

public sealed record CustomerRequestHistoryDto(
    string Event,
    string Message,
    DateTimeOffset CreatedAt);
