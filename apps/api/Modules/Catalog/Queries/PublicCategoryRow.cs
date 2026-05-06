namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record PublicCategoryRow(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? H1,
    string? Description,
    int SortOrder,
    bool IsVisibleInMenu);
