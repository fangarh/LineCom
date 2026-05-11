using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogCategorySqlTests
{
    [Fact]
    public void ListCategories_SelectsAdminFieldsAndUsageCounts()
    {
        Assert.Contains("FROM categories category", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("COUNT(product.id)::int AS \"ProductsCount\"", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("COUNT(child.id)::int AS \"ChildrenCount\"", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("ORDER BY category.parent_id NULLS FIRST, category.sort_order, category.name", AdminCatalogCategorySql.ListCategories);
    }

    [Fact]
    public void DeleteCategory_BlocksUsedCategories()
    {
        Assert.Contains("FROM products", AdminCatalogCategorySql.CountCategoryUsage);
        Assert.Contains("FROM categories child", AdminCatalogCategorySql.CountCategoryUsage);
        Assert.Contains("FROM homepage_section_items", AdminCatalogCategorySql.CountCategoryUsage);
    }
}
