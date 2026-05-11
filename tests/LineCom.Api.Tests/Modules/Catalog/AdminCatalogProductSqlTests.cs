using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogProductSqlTests
{
    [Fact]
    public void ListProducts_FiltersByAdminCatalogFieldsAndKeepsInactiveProducts()
    {
        Assert.Contains("FROM products product", AdminCatalogProductSql.ListProducts);
        Assert.Contains("INNER JOIN categories category ON category.id = product.primary_category_id", AdminCatalogProductSql.ListProducts);
        Assert.Contains("LEFT JOIN brands brand ON brand.id = product.brand_id", AdminCatalogProductSql.ListProducts);
        Assert.Contains("WHERE (@CategoryId IS NULL OR product.primary_category_id = @CategoryId)", AdminCatalogProductSql.ListProducts);
        Assert.Contains("AND (@BrandId IS NULL OR product.brand_id = @BrandId)", AdminCatalogProductSql.ListProducts);
        Assert.Contains("AND (@IsActive IS NULL OR product.is_active = @IsActive)", AdminCatalogProductSql.ListProducts);
        Assert.Contains("AND (@PublishStatus IS NULL OR product.publish_status = @PublishStatus)", AdminCatalogProductSql.ListProducts);
        Assert.Contains("product.name ILIKE '%' || @Search || '%'", AdminCatalogProductSql.ListProducts);
        Assert.DoesNotContain("product.is_active = TRUE", AdminCatalogProductSql.ListProducts);
    }

    [Fact]
    public void GetProduct_LoadsProductBrandCategoryAttributesAndImageSummary()
    {
        Assert.Contains("product.primary_category_id AS \"CategoryId\"", AdminCatalogProductSql.GetProduct);
        Assert.Contains("category.name AS \"CategoryName\"", AdminCatalogProductSql.GetProduct);
        Assert.Contains("brand.name AS \"BrandName\"", AdminCatalogProductSql.GetProduct);
        Assert.Contains("image_summary.\"ImagesCount\"", AdminCatalogProductSql.GetProduct);
        Assert.Contains("FROM product_attribute_values value", AdminCatalogProductSql.GetProductAttributes);
        Assert.Contains("INNER JOIN category_attributes attribute", AdminCatalogProductSql.GetProductAttributes);
        Assert.Contains("LEFT JOIN attribute_options option", AdminCatalogProductSql.GetProductAttributes);
        Assert.DoesNotContain("product.is_active = TRUE", AdminCatalogProductSql.GetProduct);
    }

    [Fact]
    public void DeleteUsage_CountsRequestsAndHomepageItems()
    {
        Assert.Contains(
            "(SELECT COUNT(*)::int FROM request_items item WHERE item.product_id = @Id)",
            AdminCatalogProductSql.CountProductUsage);
        Assert.Contains(
            "+ (SELECT COUNT(*)::int FROM homepage_section_items item WHERE item.product_id = @Id)",
            AdminCatalogProductSql.CountProductUsage);
    }

    [Fact]
    public void FindDuplicateHardIdentity_ChecksSlugSkuAndExternalId()
    {
        Assert.Contains("product.slug = @Slug", AdminCatalogProductSql.FindDuplicateHardIdentity);
        Assert.Contains("product.sku = @Sku", AdminCatalogProductSql.FindDuplicateHardIdentity);
        Assert.Contains("product.external_id = @ExternalId", AdminCatalogProductSql.FindDuplicateHardIdentity);
        Assert.Contains("@ExcludeProductId IS NULL OR product.id <> @ExcludeProductId", AdminCatalogProductSql.FindDuplicateHardIdentity);
    }
}
