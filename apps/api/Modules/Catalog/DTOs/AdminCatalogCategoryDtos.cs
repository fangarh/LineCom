namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminCategoryListQuery(
    int? Page,
    int? PageSize,
    Guid? ParentId,
    string? Search,
    bool? IsActive);

public sealed record AdminCategoryListResponse(
    IReadOnlyList<AdminCategoryListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminCategoryListItemDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu,
    int ProductsCount,
    int ChildrenCount);

public sealed record AdminCategoryDetailDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu,
    int ProductsCount,
    int ChildrenCount);

public sealed record UpsertAdminCategoryCommand(
    Guid? ParentId,
    string? Name,
    string? Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int? SortOrder,
    bool? IsActive,
    bool? IsVisibleInMenu);

public sealed record MoveAdminCategoryCommand(Guid? ParentId);

public sealed record SortAdminCategoryCommand(int SortOrder);
