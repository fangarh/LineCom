using Dapper;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

[Collection(PostgresMigrationCollection.Name)]
public sealed class CatalogFoundationDatabaseBehaviorTests
{
    private readonly PostgresMigrationFixture _fixture;

    public CatalogFoundationDatabaseBehaviorTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_AppliesSuccessfully()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var tableCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';");

        Assert.True(tableCount >= 9);
    }

    [Fact]
    public async Task Category_CannotUseDescendantAsParent()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@ParentId, 'Parent', 'parent');

            INSERT INTO categories (id, parent_id, name, slug)
            VALUES (@ChildId, @ParentId, 'Child', 'child');
            """,
            new { ParentId = parentId, ChildId = childId });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                "UPDATE categories SET parent_id = @ChildId WHERE id = @ParentId;",
                new { ParentId = parentId, ChildId = childId }));

        Assert.Equal("23514", exception.SqlState);
    }

    [Fact]
    public async Task Product_PrimaryCategoryCannotChangeWhenAttributeValuesExist()
    {
        if (!_fixture.IsConfigured) return;

        await using var connection = await OpenConnectionAsync();
        var sourceCategoryId = Guid.NewGuid();
        var targetCategoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES
                (@SourceCategoryId, 'Source', 'source'),
                (@TargetCategoryId, 'Target', 'target');

            INSERT INTO products (id, primary_category_id, name, slug, sale_unit, unit_quantity)
            VALUES (@ProductId, @SourceCategoryId, 'Cable', 'cable', 'coil', '305 m');

            INSERT INTO category_attributes (id, category_id, name, code, type)
            VALUES (@AttributeId, @SourceCategoryId, 'Material', 'material', 'text');

            INSERT INTO product_attribute_values (product_id, attribute_id, value_text, normalized_value)
            VALUES (@ProductId, @AttributeId, 'CU', 'cu');
            """,
            new
            {
                SourceCategoryId = sourceCategoryId,
                TargetCategoryId = targetCategoryId,
                ProductId = productId,
                AttributeId = attributeId
            });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                "UPDATE products SET primary_category_id = @TargetCategoryId WHERE id = @ProductId;",
                new { ProductId = productId, TargetCategoryId = targetCategoryId }));

        Assert.Equal("23514", exception.SqlState);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
