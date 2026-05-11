using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Tests.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogProductAttributeRepositoryDatabaseTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogProductAttributeRepositoryDatabaseTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateProductAttributesAsync_ReplacesValuesAndStoresNormalizedValues()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductWithTextAndSelectAttributesAsync(connection, publishStatus: "draft");
        var repository = CreateRepository(dataSource);

        await repository.UpdateProductAttributesAsync(
            seed.ProductId,
            [
                new AdminProductAttributeValueUpsert(seed.TextAttributeId, " XLPE ", null, null, null),
                new AdminProductAttributeValueUpsert(seed.SelectAttributeId, null, null, null, seed.NewOptionId)
            ]);

        var rows = (await connection.QueryAsync<ProductAttributeValueRow>(
            """
            SELECT
                attribute_id AS "AttributeId",
                value_text AS "ValueText",
                attribute_option_id AS "AttributeOptionId",
                normalized_value AS "NormalizedValue"
            FROM product_attribute_values
            WHERE product_id = @ProductId
            ORDER BY attribute_id;
            """,
            seed)).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Contains(rows, row =>
            row.AttributeId == seed.TextAttributeId
            && row.ValueText == " XLPE "
            && row.NormalizedValue == "xlpe");
        Assert.Contains(rows, row =>
            row.AttributeId == seed.SelectAttributeId
            && row.AttributeOptionId == seed.NewOptionId
            && row.NormalizedValue == "copper");
        Assert.DoesNotContain(rows, row => row.AttributeOptionId == seed.OldOptionId);
    }

    [Fact]
    public async Task UpdateProductAttributesAsync_InvalidOptionRollsBackAndPreservesOldValues()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductWithTextAndSelectAttributesAsync(connection, publishStatus: "draft");
        var repository = CreateRepository(dataSource);

        await Assert.ThrowsAsync<InvalidAdminProductException>(() =>
            repository.UpdateProductAttributesAsync(
                seed.ProductId,
                [
                    new AdminProductAttributeValueUpsert(seed.SelectAttributeId, null, null, null, seed.InactiveOptionId)
                ]));

        var rows = (await connection.QueryAsync<ProductAttributeValueRow>(
            """
            SELECT
                attribute_id AS "AttributeId",
                value_text AS "ValueText",
                attribute_option_id AS "AttributeOptionId",
                normalized_value AS "NormalizedValue"
            FROM product_attribute_values
            WHERE product_id = @ProductId
            ORDER BY attribute_id;
            """,
            seed)).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Contains(rows, row =>
            row.AttributeId == seed.TextAttributeId
            && row.ValueText == "PVC"
            && row.NormalizedValue == "pvc");
        Assert.Contains(rows, row =>
            row.AttributeId == seed.SelectAttributeId
            && row.AttributeOptionId == seed.OldOptionId
            && row.NormalizedValue == "aluminum");
    }

    [Fact]
    public async Task UpdateProductAttributesAsync_PublishedProductMissingRequiredAttributeRollsBack()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductWithTextAndSelectAttributesAsync(connection, publishStatus: "published");
        var repository = CreateRepository(dataSource);

        await Assert.ThrowsAsync<AdminProductNotReadyException>(() =>
            repository.UpdateProductAttributesAsync(
                seed.ProductId,
                [
                    new AdminProductAttributeValueUpsert(seed.SelectAttributeId, null, null, null, seed.NewOptionId)
                ]));

        var rows = (await connection.QueryAsync<ProductAttributeValueRow>(
            """
            SELECT
                attribute_id AS "AttributeId",
                value_text AS "ValueText",
                attribute_option_id AS "AttributeOptionId",
                normalized_value AS "NormalizedValue"
            FROM product_attribute_values
            WHERE product_id = @ProductId
            ORDER BY attribute_id;
            """,
            seed)).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Contains(rows, row =>
            row.AttributeId == seed.TextAttributeId
            && row.ValueText == "PVC"
            && row.NormalizedValue == "pvc");
        Assert.Contains(rows, row =>
            row.AttributeId == seed.SelectAttributeId
            && row.AttributeOptionId == seed.OldOptionId
            && row.NormalizedValue == "aluminum");
    }

    private static async Task<ProductAttributeSeed> SeedProductWithTextAndSelectAttributesAsync(
        NpgsqlConnection connection,
        string publishStatus)
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var textAttributeId = Guid.NewGuid();
        var selectAttributeId = Guid.NewGuid();
        var oldOptionId = Guid.NewGuid();
        var newOptionId = Guid.NewGuid();
        var inactiveOptionId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'DB Product Attribute Category', @CategorySlug);

            INSERT INTO category_attributes (
                id,
                category_id,
                name,
                code,
                type,
                is_required,
                sort_order
            )
            VALUES
                (@TextAttributeId, @CategoryId, 'Jacket', 'jacket', 'text', TRUE, 10),
                (@SelectAttributeId, @CategoryId, 'Conductor', 'conductor', 'select', FALSE, 20);

            INSERT INTO attribute_options (
                id,
                attribute_id,
                value,
                slug,
                normalized_value,
                sort_order,
                is_active
            )
            VALUES
                (@OldOptionId, @SelectAttributeId, 'Aluminum', 'aluminum', 'aluminum', 10, TRUE),
                (@NewOptionId, @SelectAttributeId, 'Copper', 'copper', 'copper', 20, TRUE),
                (@InactiveOptionId, @SelectAttributeId, 'Steel', 'steel', 'steel', 30, FALSE);

            INSERT INTO products (
                id,
                primary_category_id,
                name,
                slug,
                availability_status,
                sale_unit,
                unit_quantity,
                publish_status,
                is_active
            )
            VALUES (
                @ProductId,
                @CategoryId,
                'DB Product Attribute Test',
                @ProductSlug,
                'in_stock',
                'piece',
                '1 item',
                @PublishStatus,
                TRUE
            );

            INSERT INTO product_attribute_values (
                product_id,
                attribute_id,
                value_text,
                normalized_value
            )
            VALUES (
                @ProductId,
                @TextAttributeId,
                'PVC',
                'pvc'
            );

            INSERT INTO product_attribute_values (
                product_id,
                attribute_id,
                attribute_option_id,
                normalized_value
            )
            VALUES (
                @ProductId,
                @SelectAttributeId,
                @OldOptionId,
                'aluminum'
            );
            """,
            new
            {
                CategoryId = categoryId,
                ProductId = productId,
                TextAttributeId = textAttributeId,
                SelectAttributeId = selectAttributeId,
                OldOptionId = oldOptionId,
                NewOptionId = newOptionId,
                InactiveOptionId = inactiveOptionId,
                PublishStatus = publishStatus,
                CategorySlug = $"db-product-attribute-category-{categoryId:N}",
                ProductSlug = $"db-product-attribute-test-{productId:N}"
            });

        return new ProductAttributeSeed(
            categoryId,
            productId,
            textAttributeId,
            selectAttributeId,
            oldOptionId,
            newOptionId,
            inactiveOptionId);
    }

    private static DapperAdminCatalogProductRepository CreateRepository(NpgsqlDataSource dataSource)
    {
        return new DapperAdminCatalogProductRepository(new NpgsqlConnectionFactory(dataSource));
    }

    private sealed record ProductAttributeSeed(
        Guid CategoryId,
        Guid ProductId,
        Guid TextAttributeId,
        Guid SelectAttributeId,
        Guid OldOptionId,
        Guid NewOptionId,
        Guid InactiveOptionId);

    private sealed record ProductAttributeValueRow(
        Guid AttributeId,
        string? ValueText,
        Guid? AttributeOptionId,
        string? NormalizedValue);
}
