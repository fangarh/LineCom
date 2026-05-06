namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record PublicCategoryDetailRow(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? H1,
    string? SeoTitle,
    string? SeoDescription,
    int Depth);
