using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogCrudDatabaseBehaviorTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogCrudDatabaseBehaviorTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithChildCategory_ServiceBlocksAndRepositoryRejectsDirectDelete()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedParentWithChildCategoryAsync(connection);
        var repository = CreateCategoryRepository(dataSource);
        var service = CreateCategoryService(repository);

        var usageCount = await repository.CountCategoryUsageAsync(seed.ParentCategoryId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteCategoryAsync(new DefaultHttpContext(), seed.ParentCategoryId));
        var repositoryException = await Assert.ThrowsAsync<AdminCategoryInUseException>(() =>
            repository.DeleteCategoryAsync(seed.ParentCategoryId));

        Assert.Equal(1, usageCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.NotNull(repositoryException.InnerException);
        Assert.True(await CategoryExistsAsync(connection, seed.ParentCategoryId));
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithProduct_ServiceBlocksAndRepositoryRejectsDirectDelete()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductAsync(connection, "category-product");
        var repository = CreateCategoryRepository(dataSource);
        var service = CreateCategoryService(repository);

        var usageCount = await repository.CountCategoryUsageAsync(seed.CategoryId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteCategoryAsync(new DefaultHttpContext(), seed.CategoryId));
        var repositoryException = await Assert.ThrowsAsync<AdminCategoryInUseException>(() =>
            repository.DeleteCategoryAsync(seed.CategoryId));

        Assert.Equal(1, usageCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.NotNull(repositoryException.InnerException);
        Assert.True(await CategoryExistsAsync(connection, seed.CategoryId));
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithHomepageItem_ServiceBlocksAndRepositoryRejectsDirectDelete()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var categoryId = await SeedCategoryAsync(connection, "category-homepage");
        await SeedCategoryHomepageItemAsync(connection, categoryId);
        var repository = CreateCategoryRepository(dataSource);
        var service = CreateCategoryService(repository);

        var usageCount = await repository.CountCategoryUsageAsync(categoryId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteCategoryAsync(new DefaultHttpContext(), categoryId));
        var repositoryException = await Assert.ThrowsAsync<AdminCategoryInUseException>(() =>
            repository.DeleteCategoryAsync(categoryId));

        Assert.Equal(1, usageCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.NotNull(repositoryException.InnerException);
        Assert.True(await CategoryExistsAsync(connection, categoryId));
    }

    [Fact]
    public async Task DeleteProductAsync_WithRequestItem_ServiceBlocksAndRepositoryRejectsDirectDelete()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductAsync(connection, "product-request");
        await SeedRequestItemAsync(connection, seed);
        var repository = CreateProductRepository(dataSource);
        var service = CreateProductService(repository);

        var usageCount = await repository.CountProductUsageAsync(seed.ProductId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteProductAsync(new DefaultHttpContext(), seed.ProductId));
        var repositoryException = await Assert.ThrowsAsync<AdminProductInUseException>(() =>
            repository.DeleteProductAsync(seed.ProductId));

        Assert.Equal(1, usageCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.NotNull(repositoryException.InnerException);
        Assert.True(await ProductExistsAsync(connection, seed.ProductId));
    }

    [Fact]
    public async Task DeleteProductAsync_WithHomepageItem_ServiceBlocksAndRepositoryRejectsDirectDelete()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedProductAsync(connection, "product-homepage");
        await SeedProductHomepageItemAsync(connection, seed.ProductId);
        var repository = CreateProductRepository(dataSource);
        var service = CreateProductService(repository);

        var usageCount = await repository.CountProductUsageAsync(seed.ProductId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteProductAsync(new DefaultHttpContext(), seed.ProductId));
        var repositoryException = await Assert.ThrowsAsync<AdminProductInUseException>(() =>
            repository.DeleteProductAsync(seed.ProductId));

        Assert.Equal(1, usageCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.NotNull(repositoryException.InnerException);
        Assert.True(await ProductExistsAsync(connection, seed.ProductId));
    }

    [Fact]
    public async Task UpdateAttributeAsync_WithExistingValue_ServiceBlocksTypeChange()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedTextAttributeValueAsync(connection);
        var repository = CreateAttributeRepository(dataSource);
        var service = CreateAttributeService(repository);

        var existing = await repository.GetAttributeAsync(seed.CategoryId, seed.AttributeId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateAttributeAsync(
                new DefaultHttpContext(),
                seed.CategoryId,
                seed.AttributeId,
                new UpsertAdminCategoryAttributeCommand(
                    "Jacket",
                    "jacket",
                    "number",
                    null,
                    true,
                    false,
                    false,
                    true,
                    false,
                    false,
                    10,
                    true)));

        var attributeType = await connection.ExecuteScalarAsync<string>(
            "SELECT type FROM category_attributes WHERE id = @AttributeId;",
            seed);

        Assert.NotNull(existing);
        Assert.Equal(1, existing.ProductValuesCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.Equal("text", attributeType);
    }

    [Fact]
    public async Task DeleteOptionAsync_WithExistingValue_ServiceBlocksAndRepositoryReturnsFalse()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        var seed = await SeedSelectAttributeValueAsync(connection);
        var repository = CreateAttributeRepository(dataSource);
        var service = CreateAttributeService(repository);

        var existing = await repository.GetOptionAsync(seed.CategoryId, seed.AttributeId, seed.OptionId);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteOptionAsync(
                new DefaultHttpContext(),
                seed.CategoryId,
                seed.AttributeId,
                seed.OptionId));
        var deleted = await repository.DeleteOptionAsync(seed.CategoryId, seed.AttributeId, seed.OptionId);

        Assert.NotNull(existing);
        Assert.Equal(1, existing.ProductValuesCount);
        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.False(deleted);
        Assert.True(await OptionExistsAsync(connection, seed.OptionId));
    }

    private static async Task<ParentChildCategorySeed> SeedParentWithChildCategoryAsync(NpgsqlConnection connection)
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategoryId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES
                (@ParentCategoryId, 'DB CRUD Parent Category', @ParentSlug),
                (@ChildCategoryId, 'DB CRUD Child Category', @ChildSlug);

            UPDATE categories
            SET parent_id = @ParentCategoryId
            WHERE id = @ChildCategoryId;
            """,
            new
            {
                ParentCategoryId = parentCategoryId,
                ChildCategoryId = childCategoryId,
                ParentSlug = Slug("db-crud-parent", parentCategoryId),
                ChildSlug = Slug("db-crud-child", childCategoryId)
            });

        return new ParentChildCategorySeed(parentCategoryId, childCategoryId);
    }

    private static async Task<Guid> SeedCategoryAsync(NpgsqlConnection connection, string slugPrefix)
    {
        var categoryId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'DB CRUD Category', @CategorySlug);
            """,
            new
            {
                CategoryId = categoryId,
                CategorySlug = Slug(slugPrefix, categoryId)
            });

        return categoryId;
    }

    private static async Task<ProductSeed> SeedProductAsync(NpgsqlConnection connection, string slugPrefix)
    {
        var categoryId = await SeedCategoryAsync(connection, $"{slugPrefix}-category");
        var productId = Guid.NewGuid();
        var productSlug = Slug(slugPrefix, productId);

        await connection.ExecuteAsync(
            """
            INSERT INTO products (
                id,
                primary_category_id,
                name,
                slug,
                sku,
                availability_status,
                sale_unit,
                unit_quantity,
                publish_status,
                is_active
            )
            VALUES (
                @ProductId,
                @CategoryId,
                'DB CRUD Product',
                @ProductSlug,
                @Sku,
                'in_stock',
                'piece',
                '1 item',
                'draft',
                TRUE
            );
            """,
            new
            {
                CategoryId = categoryId,
                ProductId = productId,
                ProductSlug = productSlug,
                Sku = $"SKU-{productId:N}"
            });

        return new ProductSeed(categoryId, productId, productSlug);
    }

    private static async Task SeedCategoryHomepageItemAsync(NpgsqlConnection connection, Guid categoryId)
    {
        var sectionId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM homepage_sections WHERE code = 'direction_categories';");

        await connection.ExecuteAsync(
            """
            INSERT INTO homepage_section_items (section_id, category_id, sort_order, is_active)
            VALUES (@SectionId, @CategoryId, 10, TRUE);
            """,
            new { SectionId = sectionId, CategoryId = categoryId });
    }

    private static async Task SeedProductHomepageItemAsync(NpgsqlConnection connection, Guid productId)
    {
        var sectionId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM homepage_sections WHERE code = 'hero_products';");

        await connection.ExecuteAsync(
            """
            INSERT INTO homepage_section_items (section_id, product_id, sort_order, is_active)
            VALUES (@SectionId, @ProductId, 10, TRUE);
            """,
            new { SectionId = sectionId, ProductId = productId });
    }

    private static async Task SeedRequestItemAsync(NpgsqlConnection connection, ProductSeed seed)
    {
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var sequence = await connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(number_sequence), 0) + 1 FROM requests WHERE number_year = 2099;");

        await connection.ExecuteAsync(
            """
            INSERT INTO users (
                id,
                name,
                email,
                password_hash,
                role,
                is_active
            )
            VALUES (
                @UserId,
                'DB CRUD Customer',
                @Email,
                'hash',
                'customer',
                TRUE
            );

            INSERT INTO requests (
                id,
                number,
                number_year,
                number_sequence,
                user_id,
                status,
                source,
                customer_name,
                customer_email
            )
            VALUES (
                @RequestId,
                @Number,
                2099,
                @Sequence,
                @UserId,
                'new',
                'cart',
                'DB CRUD Customer',
                @Email
            );

            INSERT INTO request_items (
                request_id,
                product_id,
                quantity,
                product_name,
                product_slug,
                product_sku,
                category_name,
                category_slug,
                availability_status,
                sale_unit,
                unit_quantity,
                sort_order
            )
            VALUES (
                @RequestId,
                @ProductId,
                1,
                'DB CRUD Product',
                @ProductSlug,
                @Sku,
                'DB CRUD Category',
                @CategorySlug,
                'in_stock',
                'piece',
                '1 item',
                10
            );
            """,
            new
            {
                UserId = userId,
                RequestId = requestId,
                Number = $"REQ-{requestId:N}",
                Sequence = sequence,
                Email = $"crud-{userId:N}@example.test",
                seed.CategoryId,
                CategorySlug = Slug("product-request-category", seed.CategoryId),
                seed.ProductId,
                seed.ProductSlug,
                Sku = $"SKU-{seed.ProductId:N}"
            });
    }

    private static async Task<TextAttributeValueSeed> SeedTextAttributeValueAsync(NpgsqlConnection connection)
    {
        var productSeed = await SeedProductAsync(connection, "attribute-text");
        var attributeId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO category_attributes (
                id,
                category_id,
                name,
                code,
                type,
                is_required,
                sort_order,
                is_active
            )
            VALUES (
                @AttributeId,
                @CategoryId,
                'Jacket',
                'jacket',
                'text',
                TRUE,
                10,
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
                @AttributeId,
                'PVC',
                'pvc'
            );
            """,
            new
            {
                productSeed.CategoryId,
                productSeed.ProductId,
                AttributeId = attributeId
            });

        return new TextAttributeValueSeed(productSeed.CategoryId, productSeed.ProductId, attributeId);
    }

    private static async Task<SelectAttributeValueSeed> SeedSelectAttributeValueAsync(NpgsqlConnection connection)
    {
        var productSeed = await SeedProductAsync(connection, "attribute-option");
        var attributeId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO category_attributes (
                id,
                category_id,
                name,
                code,
                type,
                sort_order,
                is_active
            )
            VALUES (
                @AttributeId,
                @CategoryId,
                'Voltage',
                'voltage',
                'select',
                10,
                TRUE
            );

            INSERT INTO attribute_options (
                id,
                attribute_id,
                value,
                slug,
                normalized_value,
                sort_order,
                is_active
            )
            VALUES (
                @OptionId,
                @AttributeId,
                '220 V',
                '220-v',
                '220 v',
                10,
                TRUE
            );

            INSERT INTO product_attribute_values (
                product_id,
                attribute_id,
                attribute_option_id,
                normalized_value
            )
            VALUES (
                @ProductId,
                @AttributeId,
                @OptionId,
                '220 v'
            );
            """,
            new
            {
                productSeed.CategoryId,
                productSeed.ProductId,
                AttributeId = attributeId,
                OptionId = optionId
            });

        return new SelectAttributeValueSeed(productSeed.CategoryId, productSeed.ProductId, attributeId, optionId);
    }

    private static async Task<bool> CategoryExistsAsync(NpgsqlConnection connection, Guid categoryId)
    {
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM categories WHERE id = @CategoryId);",
            new { CategoryId = categoryId });
    }

    private static async Task<bool> ProductExistsAsync(NpgsqlConnection connection, Guid productId)
    {
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM products WHERE id = @ProductId);",
            new { ProductId = productId });
    }

    private static async Task<bool> OptionExistsAsync(NpgsqlConnection connection, Guid optionId)
    {
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM attribute_options WHERE id = @OptionId);",
            new { OptionId = optionId });
    }

    private static DapperAdminCatalogCategoryRepository CreateCategoryRepository(NpgsqlDataSource dataSource)
    {
        return new DapperAdminCatalogCategoryRepository(new NpgsqlConnectionFactory(dataSource));
    }

    private static DapperAdminCatalogProductRepository CreateProductRepository(NpgsqlDataSource dataSource)
    {
        return new DapperAdminCatalogProductRepository(new NpgsqlConnectionFactory(dataSource));
    }

    private static DapperAdminCatalogAttributeRepository CreateAttributeRepository(NpgsqlDataSource dataSource)
    {
        return new DapperAdminCatalogAttributeRepository(new NpgsqlConnectionFactory(dataSource));
    }

    private static AdminCatalogCategoryService CreateCategoryService(IAdminCatalogCategoryRepository repository)
    {
        return new AdminCatalogCategoryService(new AllowStaffGuard(), repository);
    }

    private static AdminCatalogProductService CreateProductService(IAdminCatalogProductRepository repository)
    {
        return new AdminCatalogProductService(new AllowStaffGuard(), repository, new UnusedAdminProductDuplicateQuery());
    }

    private static AdminCatalogAttributeService CreateAttributeService(IAdminCatalogAttributeRepository repository)
    {
        return new AdminCatalogAttributeService(new AllowStaffGuard(), repository);
    }

    private static string Slug(string prefix, Guid id)
    {
        return $"{prefix}-{id:N}";
    }

    private sealed class AllowStaffGuard : IAdminCatalogStaffGuard
    {
        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CurrentUserDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Admin User",
                "admin@example.test",
                null,
                "admin"));
        }
    }

    private sealed class UnusedAdminProductDuplicateQuery : IAdminProductDuplicateQuery
    {
        public Task<AdminProductDuplicateCandidatesResponse> FindCandidatesAsync(
            AdminProductDuplicateCandidateQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminProductDuplicateCandidatesResponse([]));
        }
    }

    private sealed record ParentChildCategorySeed(Guid ParentCategoryId, Guid ChildCategoryId);

    private sealed record ProductSeed(Guid CategoryId, Guid ProductId, string ProductSlug);

    private sealed record TextAttributeValueSeed(Guid CategoryId, Guid ProductId, Guid AttributeId);

    private sealed record SelectAttributeValueSeed(Guid CategoryId, Guid ProductId, Guid AttributeId, Guid OptionId);
}
