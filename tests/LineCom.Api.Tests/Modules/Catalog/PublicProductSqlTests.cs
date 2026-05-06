using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductSqlTests
{
    [Fact]
    public void SelectActiveCategoryIdBySlug_UsesParameterizedSlugAndActiveVisibility()
    {
        Assert.Contains("slug = @CategorySlug", PublicProductSql.SelectActiveCategoryIdBySlug);
        Assert.Contains("is_active = TRUE", PublicProductSql.SelectActiveCategoryIdBySlug);
    }

    [Fact]
    public void BuildProductListSql_SelectsOnlyPublishedProductsAndActiveCategories()
    {
        var sql = PublicProductSql.BuildProductListSql(string.Empty, "ORDER BY product.sort_order, product.name, product.slug");

        Assert.Contains("product.publish_status = 'published'", sql);
        Assert.Contains("category.is_active = TRUE", sql);
        Assert.Contains("brand.is_active = TRUE", sql);
    }

    [Fact]
    public void BuildProductListSql_SelectsOnlyActiveProductImages()
    {
        var sql = PublicProductSql.BuildProductListSql(string.Empty, "ORDER BY product.sort_order, product.name, product.slug");

        Assert.Contains("stored_file.status = 'active'", sql);
        Assert.Contains("stored_file.purpose = 'product_image'", sql);
        Assert.Contains("ORDER BY image.is_main DESC, image.sort_order, image.id", sql);
    }

    [Fact]
    public void BuildProductListSql_UsesParameterizedPaging()
    {
        var sql = PublicProductSql.BuildProductListSql(string.Empty, "ORDER BY product.sort_order, product.name, product.slug");

        Assert.Contains("OFFSET @Offset", sql);
        Assert.Contains("LIMIT @PageSize", sql);
    }

    [Fact]
    public void BuildSelectAttributeFilterSql_UsesParameterizedSelectAttributeFilter()
    {
        var sql = PublicProductSql.BuildSelectAttributeFilterSql(0);

        Assert.Contains("EXISTS", sql);
        Assert.Contains("product_attribute_values attribute_value", sql);
        Assert.Contains("attribute.category_id = product.primary_category_id", sql);
        Assert.Contains("attribute.is_active = TRUE", sql);
        Assert.Contains("attribute.is_filterable = TRUE", sql);
        Assert.Contains("attribute.type = 'select'", sql);
        Assert.Contains("option.is_active = TRUE", sql);
        Assert.Contains("attribute.code = @AttributeCode0", sql);
        Assert.Contains("option.slug = @AttributeOptionSlug0", sql);
    }

    [Fact]
    public void BuildProductListSql_DoesNotExposePriceOrSeoFields()
    {
        var sql = PublicProductSql.BuildProductListSql(string.Empty, "ORDER BY product.sort_order, product.name, product.slug");

        Assert.DoesNotContain("price", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("seo_title", sql);
        Assert.DoesNotContain("seo_description", sql);
    }

    [Fact]
    public void GetProductDetail_SelectsOnlyPublishedProductFromActiveCategory()
    {
        Assert.Contains("product.slug = @Slug", PublicProductSql.GetProductDetail);
        Assert.Contains("product.publish_status = 'published'", PublicProductSql.GetProductDetail);
        Assert.Contains("category.is_active = TRUE", PublicProductSql.GetProductDetail);
    }

    [Fact]
    public void GetProductDetail_HidesInactiveBrand()
    {
        Assert.Contains("LEFT JOIN brands brand ON brand.id = product.brand_id", PublicProductSql.GetProductDetail);
        Assert.Contains("brand.is_active = TRUE", PublicProductSql.GetProductDetail);
    }

    [Fact]
    public void GetProductDetail_SelectsOnlyActiveProductImages()
    {
        Assert.Contains("stored_file.status = 'active'", PublicProductSql.GetProductDetail);
        Assert.Contains("stored_file.purpose = 'product_image'", PublicProductSql.GetProductDetail);
        Assert.Contains("ORDER BY image.is_main DESC, image.sort_order, image.id", PublicProductSql.GetProductDetail);
    }

    [Fact]
    public void GetProductDetail_SelectsOnlyVisibleActiveAttributesAndActiveSelectOptions()
    {
        Assert.Contains("attribute.is_active = TRUE", PublicProductSql.GetProductDetail);
        Assert.Contains("attribute.is_visible_in_product = TRUE", PublicProductSql.GetProductDetail);
        Assert.Contains("option.is_active = TRUE", PublicProductSql.GetProductDetail);
        Assert.Contains("AND (attribute.type <> 'select' OR option.id IS NOT NULL)", PublicProductSql.GetProductDetail);
    }

    [Fact]
    public void GetProductDetail_ReturnsSeoFieldsButDoesNotExposePrice()
    {
        Assert.Contains("product.seo_title AS \"SeoTitle\"", PublicProductSql.GetProductDetail);
        Assert.Contains("product.seo_description AS \"SeoDescription\"", PublicProductSql.GetProductDetail);
        Assert.DoesNotContain("price", PublicProductSql.GetProductDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetProductDetail_PreventsRecursiveBreadcrumbCycles()
    {
        Assert.Contains("ARRAY[category.id] AS \"Path\"", PublicProductSql.GetProductDetail);
        Assert.Contains("AND NOT parent.id = ANY(child.\"Path\")", PublicProductSql.GetProductDetail);
    }
}
