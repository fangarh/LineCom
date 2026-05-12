namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminHomepageSectionsResponse(
    IReadOnlyList<AdminHomepageSectionDto> Sections);

public sealed record AdminHomepageSectionDto(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminHomepageSectionItemDto> Items);

public sealed record AdminHomepageSectionItemDto(
    Guid Id,
    Guid? ProductId,
    Guid? CategoryId,
    string Name,
    string? Slug,
    string? SecondaryText,
    int SortOrder,
    bool IsActive,
    string VisibilityStatus);

public sealed record UpdateAdminHomepageSectionCommand(
    string? Title,
    int? ItemLimit,
    int? SortOrder,
    bool? IsActive);

public sealed record CreateAdminHomepageSectionItemCommand(
    Guid? ProductId,
    Guid? CategoryId,
    int? SortOrder,
    bool? IsActive);

public sealed record UpdateAdminHomepageSectionItemCommand(
    int? SortOrder,
    bool? IsActive);

public sealed record UpdateAdminHomepageSectionItemOrderCommand(
    IReadOnlyList<Guid> ItemIds);

public sealed record PublicHomepageSectionsResponse(
    IReadOnlyList<PublicHomepageSectionDto> Sections);

public sealed record PublicHomepageSectionDto(
    string Code,
    string Title,
    string Type,
    IReadOnlyList<PublicHomepageSectionItemDto> Items);

public sealed record PublicHomepageSectionItemDto(
    Guid Id,
    Guid? ProductId,
    Guid? CategoryId,
    string Name,
    string? Slug,
    string? SecondaryText);
