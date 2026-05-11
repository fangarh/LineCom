using Dapper;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogFoundationDatabaseBehaviorTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogFoundationDatabaseBehaviorTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_AddsProductActiveFlagAndHomepageSections()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();

        var isActiveDataType = await connection.ExecuteScalarAsync<string?>(
            """
            SELECT data_type
            FROM information_schema.columns
            WHERE table_schema = 'public'
                AND table_name = 'products'
                AND column_name = 'is_active';
            """);
        var sectionCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM homepage_sections;");

        Assert.Equal("boolean", isActiveDataType);
        Assert.Equal(3, sectionCount);
    }

    [Fact]
    public async Task HomepageSectionItem_RejectsCategoryInsideProductSection()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var categoryId = Guid.NewGuid();
        var categorySlug = $"test-category-{categoryId:N}";
        var sectionId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM homepage_sections WHERE code = 'hero_products';");

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'Тестовая категория', @CategorySlug);
            """,
            new { CategoryId = categoryId, CategorySlug = categorySlug });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO homepage_section_items (section_id, category_id)
                VALUES (@SectionId, @CategoryId);
                """,
                new { SectionId = sectionId, CategoryId = categoryId }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Contains("cannot use category item", exception.Message);
    }

    [Fact]
    public async Task Products_PublishStatusRejectsArchived()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categorySlug = $"archive-test-category-{categoryId:N}";
        var productSlug = $"archive-test-product-{productId:N}";

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'Тестовая категория', @CategorySlug);
            """,
            new { CategoryId = categoryId, CategorySlug = categorySlug });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO products (
                    id,
                    primary_category_id,
                    name,
                    slug,
                    sale_unit,
                    unit_quantity,
                    publish_status
                )
                VALUES (
                    @ProductId,
                    @CategoryId,
                    'Тестовый товар',
                    @ProductSlug,
                    'coil',
                    '305 m',
                    'archived'
                );
                """,
                new
                {
                    ProductId = productId,
                    CategoryId = categoryId,
                    ProductSlug = productSlug
                }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task HomepageSection_RejectsTypeChangeWithIncompatibleItems()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categorySlug = $"type-change-category-{categoryId:N}";
        var productSlug = $"type-change-product-{productId:N}";
        var sectionId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM homepage_sections WHERE code = 'hero_products';");

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'Тестовая категория', @CategorySlug);

            INSERT INTO products (id, primary_category_id, name, slug, sale_unit, unit_quantity)
            VALUES (@ProductId, @CategoryId, 'Тестовый товар', @ProductSlug, 'coil', '305 m');

            INSERT INTO homepage_section_items (section_id, product_id)
            VALUES (@SectionId, @ProductId);
            """,
            new
            {
                CategoryId = categoryId,
                CategorySlug = categorySlug,
                ProductId = productId,
                ProductSlug = productSlug,
                SectionId = sectionId
            });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                "UPDATE homepage_sections SET type = 'category_list' WHERE id = @SectionId;",
                new { SectionId = sectionId }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Contains("cannot change to category_list", exception.Message);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
