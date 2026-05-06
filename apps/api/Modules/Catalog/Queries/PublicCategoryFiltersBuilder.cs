using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicCategoryFiltersBuilder
{
    public static PublicCategoryFiltersDto Build(
        PublicCategoryFilterCategoryRow? category,
        IReadOnlyCollection<PublicCategoryFilterRow> rows)
    {
        if (category is null)
        {
            throw PublicCatalogErrors.CategoryNotFound();
        }

        var filters = rows
            .GroupBy(row => new
            {
                row.Code,
                row.Name,
                row.Type,
                row.Unit,
                row.SortOrder
            })
            .Select(group => new PublicFilterDto(
                group.Key.Code,
                group.Key.Name,
                group.Key.Type,
                group.Key.Unit,
                group.Key.SortOrder,
                group
                    .Where(row => row.OptionValue is not null && row.OptionSlug is not null)
                    .Select(row => new PublicFilterOptionDto(
                        row.OptionValue!,
                        row.OptionSlug!,
                        row.OptionSortOrder ?? 0))
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Value)
                    .ThenBy(option => option.Slug)
                    .ToArray()))
            .OrderBy(filter => filter.SortOrder)
            .ThenBy(filter => filter.Name)
            .ThenBy(filter => filter.Code)
            .ToArray();

        return new PublicCategoryFiltersDto(
            new PublicCategorySummaryDto(category.Name, category.Slug),
            filters);
    }
}
