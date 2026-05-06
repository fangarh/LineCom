namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class CatalogFoundationMigrationTests
{
    private static readonly string CatalogFoundationSql = ReadMigration("002_catalog_foundation.sql");

    [Theory]
    [InlineData("stored_files")]
    [InlineData("categories")]
    [InlineData("brands")]
    [InlineData("products")]
    [InlineData("product_images")]
    [InlineData("category_attributes")]
    [InlineData("attribute_options")]
    [InlineData("attribute_value_aliases")]
    [InlineData("product_attribute_values")]
    public void CatalogFoundation_CreatesExpectedReleaseTables(string tableName)
    {
        Assert.Contains($"CREATE TABLE {tableName} (", CatalogFoundationSql);
    }

    [Theory]
    [InlineData("ck_stored_files_purpose CHECK (purpose IN ('product_image', 'brand_logo', 'import_source', 'export_result', 'temp'))")]
    [InlineData("ck_stored_files_status CHECK (status IN ('active', 'deleted', 'orphaned'))")]
    [InlineData("ck_products_availability_status CHECK (availability_status IN ('in_stock', 'on_order', 'check_availability'))")]
    [InlineData("ck_products_sale_unit CHECK (sale_unit IN ('coil', 'box', 'piece', 'pack'))")]
    [InlineData("ck_products_publish_status CHECK (publish_status IN ('draft', 'published', 'archived'))")]
    [InlineData("ck_category_attributes_type CHECK (type IN ('text', 'number', 'select', 'boolean'))")]
    public void CatalogFoundation_ConstrainsReleaseCodeValues(string expectedConstraint)
    {
        Assert.Contains(expectedConstraint, CatalogFoundationSql);
    }

    [Theory]
    [InlineData("CREATE UNIQUE INDEX ux_categories_slug ON categories (slug);")]
    [InlineData("CREATE UNIQUE INDEX ux_brands_slug ON brands (slug);")]
    [InlineData("CREATE UNIQUE INDEX ux_products_slug ON products (slug);")]
    [InlineData("CREATE UNIQUE INDEX ux_products_sku ON products (sku) WHERE sku IS NOT NULL;")]
    [InlineData("CREATE UNIQUE INDEX ux_products_external_id ON products (external_id) WHERE external_id IS NOT NULL;")]
    [InlineData("CREATE UNIQUE INDEX ux_product_images_single_main ON product_images (product_id) WHERE is_main;")]
    [InlineData("CREATE UNIQUE INDEX ux_product_attribute_values_product_id_attribute_id")]
    public void CatalogFoundation_DefinesPublicIdentityAndCardinalityIndexes(string expectedIndex)
    {
        Assert.Contains(expectedIndex, CatalogFoundationSql);
    }

    [Fact]
    public void CatalogFoundation_StoresProductAttributeValueInOnlyOneTypedColumn()
    {
        var normalizedSql = CatalogFoundationSql.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains(
            "CONSTRAINT ck_product_attribute_values_single_storage_column CHECK (\n        num_nonnulls(value_text, value_number, value_boolean, attribute_option_id) = 1\n    )",
            normalizedSql);
    }

    [Theory]
    [InlineData("CREATE OR REPLACE FUNCTION validate_brand_logo_file()")]
    [InlineData("CREATE OR REPLACE FUNCTION validate_product_image_file()")]
    [InlineData("CREATE OR REPLACE FUNCTION validate_attribute_option_attribute()")]
    [InlineData("CREATE OR REPLACE FUNCTION validate_product_attribute_value()")]
    [InlineData("CREATE TRIGGER trg_brands_validate_logo_file")]
    [InlineData("CREATE TRIGGER trg_product_images_validate_file")]
    [InlineData("CREATE TRIGGER trg_attribute_value_aliases_validate_option")]
    [InlineData("CREATE TRIGGER trg_product_attribute_values_validate")]
    public void CatalogFoundation_DefinesCrossTableValidationTriggers(string expectedSql)
    {
        Assert.Contains(expectedSql, CatalogFoundationSql);
    }

    [Theory]
    [InlineData("CREATE INDEX ix_products_public_listing ON products (publish_status, availability_status, primary_category_id, sort_order);")]
    [InlineData("CREATE INDEX ix_category_attributes_filterable ON category_attributes (category_id, is_filterable, sort_order);")]
    [InlineData("CREATE INDEX ix_product_attribute_values_attribute_id_value_number")]
    [InlineData("CREATE INDEX ix_product_attribute_values_attribute_id_normalized_value")]
    public void CatalogFoundation_DefinesIndexesForPublicCatalogReadPaths(string expectedIndex)
    {
        Assert.Contains(expectedIndex, CatalogFoundationSql);
    }

    [Fact]
    public void CatalogFoundation_DoesNotAddPublicPriceColumns()
    {
        Assert.DoesNotContain("price", CatalogFoundationSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CatalogFoundation_DoesNotUseJsonbForProductModel()
    {
        Assert.DoesNotContain("jsonb", CatalogFoundationSql, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadMigration(string fileName)
    {
        var migrationFile = Path.Combine(FindRepositoryRoot(), "apps", "dbmigrator", "Migrations", fileName);

        return File.ReadAllText(migrationFile);
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
