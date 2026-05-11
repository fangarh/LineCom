namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class AdminCatalogFoundationMigrationTests
{
    private static readonly string MigrationSql = ReadMigration("007_admin_catalog_foundation.sql");

    [Fact]
    public void AdminCatalogFoundation_EnablesPgTrgmForDuplicateSearch()
    {
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pg_trgm;", MigrationSql);
    }

    [Fact]
    public void AdminCatalogFoundation_AddsProductActiveFlag()
    {
        Assert.Contains(
            "ALTER TABLE products ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;",
            MigrationSql);
    }

    [Fact]
    public void AdminCatalogFoundation_RemovesArchivedPublishStatus()
    {
        Assert.Contains("ALTER TABLE products DROP CONSTRAINT IF EXISTS ck_products_publish_status;", MigrationSql);
        Assert.Contains(
            "ADD CONSTRAINT ck_products_publish_status CHECK (publish_status IN ('draft', 'published'))",
            MigrationSql);
        Assert.DoesNotContain("'archived'", MigrationSql);
    }

    [Theory]
    [InlineData("CREATE TABLE homepage_sections (")]
    [InlineData("CREATE TABLE homepage_section_items (")]
    [InlineData("CONSTRAINT ck_homepage_sections_type CHECK (type IN ('product_list', 'category_list'))")]
    [InlineData("CONSTRAINT ck_homepage_sections_item_limit_positive CHECK (item_limit > 0)")]
    [InlineData("CONSTRAINT ck_homepage_section_items_single_target CHECK (num_nonnulls(product_id, category_id) = 1)")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_sections_code ON homepage_sections (code);")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_section_items_section_product")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_section_items_section_category")]
    public void AdminCatalogFoundation_DefinesHomepageSectionSchema(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
    }

    [Theory]
    [InlineData("('hero_products', 'Hero: ходовые позиции', 'product_list', 3, 10, TRUE)")]
    [InlineData("('featured_products', 'Популярные позиции', 'product_list', 8, 20, TRUE)")]
    [InlineData("('direction_categories', 'Направления', 'category_list', 4, 30, TRUE)")]
    public void AdminCatalogFoundation_SeedsKnownHomepageSections(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
    }

    [Theory]
    [InlineData("CREATE INDEX IF NOT EXISTS ix_products_name_trgm ON products USING gin (name gin_trgm_ops);")]
    [InlineData("CREATE INDEX IF NOT EXISTS ix_products_slug_trgm ON products USING gin (slug gin_trgm_ops);")]
    public void AdminCatalogFoundation_AddsTrigramIndexesForDuplicateCandidates(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
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
