namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record PublicCategoryFilterCategoryRow(
    string Name,
    string Slug);

internal sealed record PublicCategoryFilterRow(
    string Code,
    string Name,
    string Type,
    string? Unit,
    int SortOrder,
    string? OptionValue,
    string? OptionSlug,
    int? OptionSortOrder);
