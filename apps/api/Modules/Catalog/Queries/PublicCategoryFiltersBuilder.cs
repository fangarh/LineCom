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

        var filters = BuildFilters(rows);

        return new PublicCategoryFiltersDto(
            new PublicCategorySummaryDto(category.Name, category.Slug),
            filters);
    }

    public static IReadOnlyList<PublicFilterDto> BuildFilters(IReadOnlyCollection<PublicCategoryFilterRow> rows)
    {
        return rows
            .GroupBy(row => new
            {
                row.Code,
                row.Type,
                row.Unit
            })
            .Select(group => new PublicFilterDto(
                group.Key.Code,
                ResolveFilterName(group.Key.Code, group),
                group.Key.Type,
                group.Key.Unit,
                group.Min(row => row.SortOrder),
                group
                    .Where(row => row.OptionValue is not null && row.OptionSlug is not null)
                    .GroupBy(row => row.OptionSlug!, StringComparer.Ordinal)
                    .Select(optionGroup =>
                    {
                        var optionRow = optionGroup
                            .OrderBy(row => row.OptionSortOrder ?? 0)
                            .ThenBy(row => row.OptionValue)
                            .First();

                        return new PublicFilterOptionDto(
                            optionRow.OptionValue!,
                            optionGroup.Key,
                            optionGroup.Min(row => row.OptionSortOrder ?? 0));
                    })
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Value)
                    .ThenBy(option => option.Slug)
                    .ToArray()))
            .OrderBy(filter => filter.SortOrder)
            .ThenBy(filter => filter.Name)
            .ThenBy(filter => filter.Code)
            .ToArray();
    }

    private static string ResolveFilterName(
        string code,
        IEnumerable<PublicCategoryFilterRow> rows)
    {
        return code switch
        {
            "application" => "Применение",
            "conductor-material" => "Материал проводника",
            "construction" => "Конструкция",
            "support-element" => "Несущий элемент",
            "cable-category" => "Категория кабеля",
            "jacket-material" => "Материал оболочки",
            "connector-type" => "Тип разъема",
            "fiber-type" => "Тип волокна",
            "form-factor" => "Форм-фактор",
            "color" => "Цвет",
            _ => rows
                .OrderBy(row => row.SortOrder)
                .ThenBy(row => row.Name)
                .Select(row => row.Name)
                .First()
        };
    }
}
