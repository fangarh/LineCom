using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicCategoryFiltersBuilderTests
{
    [Fact]
    public void Build_ReturnsCategoryFiltersWithSelectOptionsAndEmptyOptionsForOtherTypes()
    {
        var filters = PublicCategoryFiltersBuilder.Build(
            new PublicCategoryFilterCategoryRow("Витая пара", "vitaya-para"),
            [
                CreateRow(
                    "outdoor",
                    "Для улицы",
                    "boolean",
                    sortOrder: 20),
                CreateRow(
                    "conductor-material",
                    "Материал проводника",
                    "select",
                    sortOrder: 10,
                    optionValue: "CCA",
                    optionSlug: "cca",
                    optionSortOrder: 20),
                CreateRow(
                    "conductor-material",
                    "Материал проводника",
                    "select",
                    sortOrder: 10,
                    optionValue: "CU",
                    optionSlug: "cu",
                    optionSortOrder: 10)
            ]);

        Assert.Equal("vitaya-para", filters.Category.Slug);
        Assert.Equal(["conductor-material", "outdoor"], filters.Filters.Select(filter => filter.Code).ToArray());

        var selectFilter = filters.Filters[0];
        Assert.Equal(["cu", "cca"], selectFilter.Options.Select(option => option.Slug).ToArray());

        var booleanFilter = filters.Filters[1];
        Assert.Empty(booleanFilter.Options);
    }

    [Fact]
    public void BuildFilters_MergesGlobalRowsByCodeAndUsesCanonicalTechnicalNames()
    {
        var filters = PublicCategoryFiltersBuilder.BuildFilters(
        [
            CreateRow(
                "conductor-material",
                "Conductor material",
                "select",
                sortOrder: 10,
                optionValue: "Cu",
                optionSlug: "cu",
                optionSortOrder: 10),
            CreateRow(
                "conductor-material",
                "Материал проводника",
                "select",
                sortOrder: 30,
                optionValue: "CCA",
                optionSlug: "cca",
                optionSortOrder: 20)
        ]);

        var filter = Assert.Single(filters);
        Assert.Equal("Материал проводника", filter.Name);
        Assert.Equal(10, filter.SortOrder);
        Assert.Equal(["cu", "cca"], filter.Options.Select(option => option.Slug).ToArray());
    }

    [Fact]
    public void Build_ThrowsCategoryNotFound_WhenCategoryRowIsMissing()
    {
        var exception = Assert.Throws<ApiException>(() => PublicCategoryFiltersBuilder.Build(null, []));

        Assert.Equal("catalog.category_not_found", exception.Code);
        Assert.Equal("Категория не найдена.", exception.Message);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    private static PublicCategoryFilterRow CreateRow(
        string code,
        string name,
        string type,
        int sortOrder,
        string? optionValue = null,
        string? optionSlug = null,
        int? optionSortOrder = null)
    {
        return new PublicCategoryFilterRow(
            code,
            name,
            type,
            Unit: null,
            sortOrder,
            optionValue,
            optionSlug,
            optionSortOrder);
    }
}
