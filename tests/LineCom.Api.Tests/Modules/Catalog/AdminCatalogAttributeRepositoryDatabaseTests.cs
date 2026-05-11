using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Tests.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogAttributeRepositoryDatabaseTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogAttributeRepositoryDatabaseTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InheritFromParentAsync_CopiesSelectAttributesAndOptions()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedParentChildSelectAttributeAsync(connection);
        var repository = CreateRepository(dataSource);

        var result = await repository.InheritFromParentAsync(seed.ChildCategoryId);

        Assert.Equal(1, result.Added);
        Assert.Equal(0, result.Skipped);

        var inheritedAttributeId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT id
            FROM category_attributes
            WHERE category_id = @ChildCategoryId
                AND code = 'voltage';
            """,
            seed);

        var inheritedOptionSlug = await connection.ExecuteScalarAsync<string>(
            """
            SELECT slug
            FROM attribute_options
            WHERE attribute_id = @InheritedAttributeId;
            """,
            new { InheritedAttributeId = inheritedAttributeId });

        Assert.Equal("220-v", inheritedOptionSlug);
    }

    [Fact]
    public async Task InheritFromParentAsync_RepeatedCallSkipsDuplicatesByCode()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedParentChildSelectAttributeAsync(connection);
        var repository = CreateRepository(dataSource);

        await repository.InheritFromParentAsync(seed.ChildCategoryId);
        var result = await repository.InheritFromParentAsync(seed.ChildCategoryId);

        var childAttributeCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM category_attributes
            WHERE category_id = @ChildCategoryId
                AND code = 'voltage';
            """,
            seed);

        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, childAttributeCount);
    }

    [Fact]
    public async Task DeleteOptionAsync_RemovesAliasesForUnusedOption()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedCategorySelectOptionAsync(connection);
        await connection.ExecuteAsync(
            """
            INSERT INTO attribute_value_aliases (
                attribute_id,
                option_id,
                alias,
                normalized_alias
            )
            VALUES (
                @AttributeId,
                @OptionId,
                '220 volt',
                '220 volt'
            );
            """,
            seed);
        var repository = CreateRepository(dataSource);

        var deleted = await repository.DeleteOptionAsync(seed.CategoryId, seed.AttributeId, seed.OptionId);

        var aliasCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM attribute_value_aliases WHERE option_id = @OptionId;",
            seed);
        var optionCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM attribute_options WHERE id = @OptionId;",
            seed);

        Assert.True(deleted);
        Assert.Equal(0, aliasCount);
        Assert.Equal(0, optionCount);
    }

    [Fact]
    public async Task DeleteOptionAsync_ReturnsFalseForUsedOption()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedCategorySelectOptionAsync(connection);
        var productId = Guid.NewGuid();
        await connection.ExecuteAsync(
            """
            INSERT INTO products (
                id,
                primary_category_id,
                name,
                slug,
                sale_unit,
                unit_quantity
            )
            VALUES (
                @ProductId,
                @CategoryId,
                'DB Test Product',
                @ProductSlug,
                'piece',
                '1 item'
            );

            INSERT INTO product_attribute_values (
                product_id,
                attribute_id,
                attribute_option_id
            )
            VALUES (
                @ProductId,
                @AttributeId,
                @OptionId
            );
            """,
            new
            {
                seed.CategoryId,
                seed.AttributeId,
                seed.OptionId,
                ProductId = productId,
                ProductSlug = $"db-test-product-{productId:N}"
            });
        var repository = CreateRepository(dataSource);

        var deleted = await repository.DeleteOptionAsync(seed.CategoryId, seed.AttributeId, seed.OptionId);

        var optionCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM attribute_options WHERE id = @OptionId;",
            seed);

        Assert.False(deleted);
        Assert.Equal(1, optionCount);
    }

    private static async Task<ParentChildSeed> SeedParentChildSelectAttributeAsync(NpgsqlConnection connection)
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategoryId = Guid.NewGuid();
        var parentAttributeId = Guid.NewGuid();
        var parentOptionId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES
                (@ParentCategoryId, 'DB Test Parent', @ParentSlug),
                (@ChildCategoryId, 'DB Test Child', @ChildSlug);

            UPDATE categories
            SET parent_id = @ParentCategoryId
            WHERE id = @ChildCategoryId;

            INSERT INTO category_attributes (
                id,
                category_id,
                name,
                code,
                type,
                unit,
                sort_order
            )
            VALUES (
                @ParentAttributeId,
                @ParentCategoryId,
                'Voltage',
                'voltage',
                'select',
                'V',
                10
            );

            INSERT INTO attribute_options (
                id,
                attribute_id,
                value,
                slug,
                normalized_value,
                sort_order
            )
            VALUES (
                @ParentOptionId,
                @ParentAttributeId,
                '220 V',
                '220-v',
                '220 v',
                10
            );
            """,
            new
            {
                ParentCategoryId = parentCategoryId,
                ChildCategoryId = childCategoryId,
                ParentAttributeId = parentAttributeId,
                ParentOptionId = parentOptionId,
                ParentSlug = $"db-test-parent-{parentCategoryId:N}",
                ChildSlug = $"db-test-child-{childCategoryId:N}"
            });

        return new ParentChildSeed(parentCategoryId, childCategoryId, parentAttributeId, parentOptionId);
    }

    private static async Task<OptionSeed> SeedCategorySelectOptionAsync(NpgsqlConnection connection)
    {
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'DB Test Category', @CategorySlug);

            INSERT INTO category_attributes (
                id,
                category_id,
                name,
                code,
                type,
                unit,
                sort_order
            )
            VALUES (
                @AttributeId,
                @CategoryId,
                'Voltage',
                'voltage',
                'select',
                'V',
                10
            );

            INSERT INTO attribute_options (
                id,
                attribute_id,
                value,
                slug,
                normalized_value,
                sort_order
            )
            VALUES (
                @OptionId,
                @AttributeId,
                '220 V',
                '220-v',
                '220 v',
                10
            );
            """,
            new
            {
                CategoryId = categoryId,
                AttributeId = attributeId,
                OptionId = optionId,
                CategorySlug = $"db-test-category-{categoryId:N}"
            });

        return new OptionSeed(categoryId, attributeId, optionId);
    }

    private static DapperAdminCatalogAttributeRepository CreateRepository(NpgsqlDataSource dataSource)
    {
        return new DapperAdminCatalogAttributeRepository(new NpgsqlConnectionFactory(dataSource));
    }

    private sealed record ParentChildSeed(
        Guid ParentCategoryId,
        Guid ChildCategoryId,
        Guid ParentAttributeId,
        Guid ParentOptionId);

    private sealed record OptionSeed(
        Guid CategoryId,
        Guid AttributeId,
        Guid OptionId);
}
