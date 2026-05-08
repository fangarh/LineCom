namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record PublicCategoryTreeResponse(
    IReadOnlyList<PublicCategoryTreeItemDto> Items);

public sealed record PublicCategoryTreeItemDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? H1,
    string? Description,
    int SortOrder,
    bool IsVisibleInMenu,
    IReadOnlyList<PublicCategoryTreeItemDto> Children);

public sealed record PublicCategoryDetailDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? H1,
    PublicSeoDto Seo,
    IReadOnlyList<PublicBreadcrumbDto> Breadcrumbs);

public sealed record PublicCategoryFiltersDto(
    PublicCategorySummaryDto Category,
    IReadOnlyList<PublicFilterDto> Filters);

public sealed record PublicCatalogFiltersDto(
    IReadOnlyList<PublicFilterDto> Filters);

public sealed record PublicFilterDto(
    string Code,
    string Name,
    string Type,
    string? Unit,
    int SortOrder,
    IReadOnlyList<PublicFilterOptionDto> Options);

public sealed record PublicFilterOptionDto(
    string Value,
    string Slug,
    int SortOrder);
