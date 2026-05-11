namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminBrandListQuery(int? Page, int? PageSize, string? Search, bool? IsActive);

public sealed record AdminBrandListResponse(
    IReadOnlyList<AdminBrandListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminBrandListItemDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int ProductsCount);

public sealed record AdminBrandDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    Guid? LogoFileId,
    bool IsActive,
    int ProductsCount);

public sealed record UpsertAdminBrandCommand(
    string? Name,
    string? Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    Guid? LogoFileId,
    bool? IsActive);

public sealed record QuickCreateAdminBrandCommand(string? Name);
