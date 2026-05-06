using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicCategorySqlTests
{
    [Fact]
    public void GetActiveCategories_SelectsOnlyActiveCategories()
    {
        Assert.Contains("WHERE is_active = TRUE", PublicCategorySql.GetActiveCategories);
    }

    [Fact]
    public void GetActiveCategories_UsesPublicCategorySortOrder()
    {
        Assert.Contains(
            "ORDER BY parent_id NULLS FIRST, sort_order, name, slug",
            PublicCategorySql.GetActiveCategories);
    }

    [Fact]
    public void GetActiveCategories_DoesNotExposeSeoOrPriceFields()
    {
        Assert.DoesNotContain("seo_title", PublicCategorySql.GetActiveCategories);
        Assert.DoesNotContain("seo_description", PublicCategorySql.GetActiveCategories);
        Assert.DoesNotContain("price", PublicCategorySql.GetActiveCategories, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetActiveCategoryBreadcrumbs_SelectsTargetByParameterizedSlugAndActiveVisibility()
    {
        Assert.Contains("WHERE slug = @Slug", PublicCategorySql.GetActiveCategoryBreadcrumbs);
        Assert.Contains("AND is_active = TRUE", PublicCategorySql.GetActiveCategoryBreadcrumbs);
        Assert.Contains("WHERE parent.is_active = TRUE", PublicCategorySql.GetActiveCategoryBreadcrumbs);
    }

    [Fact]
    public void GetActiveCategoryBreadcrumbs_ReturnsSeoFieldsAndCanonicalInputs()
    {
        Assert.Contains("seo_title AS \"SeoTitle\"", PublicCategorySql.GetActiveCategoryBreadcrumbs);
        Assert.Contains("seo_description AS \"SeoDescription\"", PublicCategorySql.GetActiveCategoryBreadcrumbs);
        Assert.DoesNotContain("price", PublicCategorySql.GetActiveCategoryBreadcrumbs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetActiveCategoryBreadcrumbs_PreventsRecursiveCycles()
    {
        Assert.Contains("ARRAY[id] AS \"Path\"", PublicCategorySql.GetActiveCategoryBreadcrumbs);
        Assert.Contains("AND NOT parent.id = ANY(child.\"Path\")", PublicCategorySql.GetActiveCategoryBreadcrumbs);
    }

    [Fact]
    public void GetActiveCategoryFilters_SelectsTargetByParameterizedSlugAndActiveVisibility()
    {
        Assert.Contains("WHERE slug = @Slug", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("AND is_active = TRUE", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("WHERE category.slug = @Slug", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("AND category.is_active = TRUE", PublicCategorySql.GetActiveCategoryFilters);
    }

    [Fact]
    public void GetActiveCategoryFilters_SelectsOnlyActiveFilterableAttributesAndActiveOptions()
    {
        Assert.Contains("attribute.is_active = TRUE", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("attribute.is_filterable = TRUE", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("option.is_active = TRUE", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("attribute.type = 'select'", PublicCategorySql.GetActiveCategoryFilters);
    }

    [Fact]
    public void GetActiveCategoryFilters_UsesPublicFilterSortOrder()
    {
        Assert.Contains("attribute.sort_order", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("attribute.name", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("attribute.code", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("option.sort_order", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("option.value", PublicCategorySql.GetActiveCategoryFilters);
        Assert.Contains("option.slug", PublicCategorySql.GetActiveCategoryFilters);
    }

    [Fact]
    public void GetActiveCategoryFilters_DoesNotExposeSeoOrPriceFields()
    {
        Assert.DoesNotContain("seo_title", PublicCategorySql.GetActiveCategoryFilters);
        Assert.DoesNotContain("seo_description", PublicCategorySql.GetActiveCategoryFilters);
        Assert.DoesNotContain("price", PublicCategorySql.GetActiveCategoryFilters, StringComparison.OrdinalIgnoreCase);
    }
}
