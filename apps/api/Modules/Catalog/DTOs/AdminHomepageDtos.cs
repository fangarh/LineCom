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
