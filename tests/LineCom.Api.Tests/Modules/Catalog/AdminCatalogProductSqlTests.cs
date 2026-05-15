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
        Assert.Contains("(ARRAY_AGG(image.stored_file_id) FILTER (WHERE image.is_main))[1] AS \"MainImageFileId\"", AdminCatalogProductSql.GetProduct);
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

    [Fact]
    public void UpdateAttributes_LocksProductDeletesPreviousValuesAndInsertsNewValues()
    {
        Assert.Contains("FROM products product", AdminCatalogProductSql.LockProductForAttributeUpdate);
        Assert.Contains("FOR UPDATE", AdminCatalogProductSql.LockProductForAttributeUpdate);
        Assert.Contains("DELETE FROM product_attribute_values", AdminCatalogProductSql.DeleteProductAttributes);
        Assert.Contains("WHERE product_id = @ProductId", AdminCatalogProductSql.DeleteProductAttributes);
        Assert.Contains("INSERT INTO product_attribute_values", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("@ProductId", AdminCatalogProductSql.InsertProductAttributeValue);
    }

    [Fact]
    public void UpdateProduct_ClearsAttributeValuesOnlyWhenCategoryChanges()
    {
        Assert.Contains("DELETE FROM product_attribute_values", AdminCatalogProductSql.DeleteProductAttributesOnCategoryChange);
        Assert.Contains("product_id = @Id", AdminCatalogProductSql.DeleteProductAttributesOnCategoryChange);
        Assert.Contains("product.primary_category_id <> @CategoryId", AdminCatalogProductSql.DeleteProductAttributesOnCategoryChange);
    }

    [Fact]
    public void UpdateAttributes_ValidatesCategoryTypeAndActiveSelectOption()
    {
        Assert.Contains("attribute.category_id = product.primary_category_id", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("attribute.is_active = TRUE", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("attribute.type = 'text' AND @ValueText IS NOT NULL", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("attribute.type = 'number' AND @ValueNumber IS NOT NULL", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("attribute.type = 'boolean' AND @ValueBoolean IS NOT NULL", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("attribute.type = 'select'", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("option.attribute_id = attribute.id", AdminCatalogProductSql.InsertProductAttributeValue);
        Assert.Contains("option.is_active = TRUE", AdminCatalogProductSql.InsertProductAttributeValue);
    }

    [Fact]
    public void UpdateAttributes_RepositoryReplacesValuesInTransaction()
    {
        var repositorySource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "apps",
            "api",
            "Modules",
            "Catalog",
            "Repositories",
            "DapperAdminCatalogProductRepository.cs"));

        Assert.Contains("BeginTransactionAsync", repositorySource);
        Assert.Contains("AdminCatalogProductSql.LockProductForAttributeUpdate", repositorySource);
        Assert.Contains("AdminCatalogProductSql.DeleteProductAttributes", repositorySource);
        Assert.Contains("AdminCatalogProductSql.InsertProductAttributeValue", repositorySource);
        Assert.Contains("CommitAsync", repositorySource);
        Assert.Contains("RollbackAsync", repositorySource);
    }

    [Fact]
    public void UpdateAttributes_ChecksPublishedProductReadinessBeforeCommit()
    {
        Assert.Contains("product.publish_status = 'published'", AdminCatalogProductSql.CountBlockingAttributeReadinessIssues);
        Assert.Contains("required_attribute.is_required = TRUE", AdminCatalogProductSql.CountBlockingAttributeReadinessIssues);
        Assert.Contains("required_attribute.is_active = TRUE", AdminCatalogProductSql.CountBlockingAttributeReadinessIssues);
        Assert.Contains("AND value.id IS NULL", AdminCatalogProductSql.CountBlockingAttributeReadinessIssues);
        Assert.Contains("attribute.type = 'select' AND value.attribute_option_id IS NOT NULL AND option.id IS NOT NULL", AdminCatalogProductSql.CountBlockingAttributeReadinessIssues);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
