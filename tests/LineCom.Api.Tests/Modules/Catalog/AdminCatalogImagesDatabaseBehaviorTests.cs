using Dapper;
using LineCom.Api.Tests.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogImagesDatabaseBehaviorTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogImagesDatabaseBehaviorTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProductImages_AllowExactlyOneMainImagePerProduct()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync();
        var ids = await SeedProductWithTwoStoredProductImagesAsync(connection);

        await connection.ExecuteAsync(
            """
            INSERT INTO product_images (product_id, stored_file_id, alt, is_main)
            VALUES (@ProductId, @FileId, 'first', TRUE);
            """,
            new { ids.ProductId, FileId = ids.FirstFileId });

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO product_images (product_id, stored_file_id, alt, is_main)
            VALUES (@ProductId, @FileId, 'second', TRUE);
            """,
            new { ids.ProductId, FileId = ids.SecondFileId }));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }

    [Fact]
    public async Task ProductImages_PreserveSortOrderAndRejectDuplicateStoredFileForSameProduct()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync();
        var ids = await SeedProductWithTwoStoredProductImagesAsync(connection);

        await connection.ExecuteAsync(
            """
            INSERT INTO product_images (product_id, stored_file_id, alt, sort_order, is_main)
            VALUES
                (@ProductId, @FirstFileId, 'first', 20, TRUE),
                (@ProductId, @SecondFileId, 'second', 10, FALSE);
            """,
            ids);

        var orderedFileIds = (await connection.QueryAsync<Guid>(
            """
            SELECT stored_file_id
            FROM product_images
            WHERE product_id = @ProductId
            ORDER BY sort_order, id;
            """,
            new { ids.ProductId })).ToArray();

        Assert.Equal(new[] { ids.SecondFileId, ids.FirstFileId }, orderedFileIds);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO product_images (product_id, stored_file_id, alt)
            VALUES (@ProductId, @FileId, 'duplicate');
            """,
            new { ids.ProductId, FileId = ids.FirstFileId }));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }

    [Fact]
    public async Task ProductImages_RejectBrandLogoStoredFile()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync();
        var ids = await SeedProductWithStoredFileAsync(connection, "brand_logo");

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO product_images (product_id, stored_file_id, alt, is_main)
            VALUES (@ProductId, @FileId, 'bad', TRUE);
            """,
            new { ids.ProductId, ids.FileId }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task BrandLogo_AllowsValidReplaceAndRejectsInvalidReplaceWithoutChangingBrand()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync();
        var brandId = await SeedBrandAsync(connection);
        var firstLogoFileId = await SeedStoredFileAsync(connection, "brand_logo");
        var secondLogoFileId = await SeedStoredFileAsync(connection, "brand_logo");
        var productImageFileId = await SeedStoredFileAsync(connection, "product_image");

        await connection.ExecuteAsync(
            """
            UPDATE brands
            SET logo_file_id = @FileId
            WHERE id = @BrandId;
            """,
            new { BrandId = brandId, FileId = firstLogoFileId });
        await connection.ExecuteAsync(
            """
            UPDATE brands
            SET logo_file_id = @FileId
            WHERE id = @BrandId;
            """,
            new { BrandId = brandId, FileId = secondLogoFileId });

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            UPDATE brands
            SET logo_file_id = @FileId
            WHERE id = @BrandId;
            """,
            new { BrandId = brandId, FileId = productImageFileId }));

        var currentLogoFileId = await connection.QuerySingleAsync<Guid?>(
            """
            SELECT logo_file_id
            FROM brands
            WHERE id = @BrandId;
            """,
            new { BrandId = brandId });

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(secondLogoFileId, currentLogoFileId);
    }

    [Fact]
    public async Task BrandLogo_RejectsProductImageStoredFile()
    {
        if (!_fixture.IsConfigured)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync();
        var ids = await SeedBrandWithStoredFileAsync(connection, "product_image");

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            UPDATE brands
            SET logo_file_id = @FileId
            WHERE id = @BrandId;
            """,
            new { ids.BrandId, ids.FileId }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<ProductWithTwoStoredImagesSeed> SeedProductWithTwoStoredProductImagesAsync(
        NpgsqlConnection connection)
    {
        var product = await SeedProductAsync(connection);
        var firstFileId = await SeedStoredFileAsync(connection, "product_image");
        var secondFileId = await SeedStoredFileAsync(connection, "product_image");

        return new ProductWithTwoStoredImagesSeed(product.ProductId, firstFileId, secondFileId);
    }

    private static async Task<ProductWithStoredFileSeed> SeedProductWithStoredFileAsync(
        NpgsqlConnection connection,
        string filePurpose)
    {
        var product = await SeedProductAsync(connection);
        var fileId = await SeedStoredFileAsync(connection, filePurpose);

        return new ProductWithStoredFileSeed(product.ProductId, fileId);
    }

    private static async Task<BrandWithStoredFileSeed> SeedBrandWithStoredFileAsync(
        NpgsqlConnection connection,
        string filePurpose)
    {
        var brandId = await SeedBrandAsync(connection);
        var fileId = await SeedStoredFileAsync(connection, filePurpose);

        return new BrandWithStoredFileSeed(brandId, fileId);
    }

    private static async Task<Guid> SeedBrandAsync(NpgsqlConnection connection)
    {
        var brandId = Guid.NewGuid();
        var brandSlug = UniqueSlug("image-safety-brand");

        await connection.ExecuteAsync(
            """
            INSERT INTO brands (id, name, slug)
            VALUES (@BrandId, 'Image Safety Brand', @BrandSlug);
            """,
            new { BrandId = brandId, BrandSlug = brandSlug });

        return brandId;
    }

    private static async Task<ProductSeed> SeedProductAsync(NpgsqlConnection connection)
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categorySlug = UniqueSlug("image-safety-category");
        var productSlug = UniqueSlug("image-safety-product");

        await connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, slug)
            VALUES (@CategoryId, 'Image Safety Category', @CategorySlug);

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
                'Image Safety Product',
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
                CategorySlug = categorySlug,
                ProductSlug = productSlug,
                Sku = $"SKU-{productId:N}"
            });

        return new ProductSeed(categoryId, productId);
    }

    private static async Task<Guid> SeedStoredFileAsync(NpgsqlConnection connection, string purpose)
    {
        var fileId = Guid.NewGuid();
        var fileToken = Guid.NewGuid().ToString("N");

        await connection.ExecuteAsync(
            """
            INSERT INTO stored_files (
                id,
                storage_key,
                original_file_name,
                content_type,
                size_bytes,
                checksum,
                purpose,
                status
            )
            VALUES (
                @FileId,
                @StorageKey,
                @OriginalFileName,
                'image/jpeg',
                10,
                @Checksum,
                @Purpose,
                'active'
            );
            """,
            new
            {
                FileId = fileId,
                StorageKey = $"storage/test/{fileToken}.jpg",
                OriginalFileName = $"{fileToken}.jpg",
                Checksum = $"checksum-{fileToken}",
                Purpose = purpose
            });

        return fileId;
    }

    private static string UniqueSlug(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed record ProductSeed(Guid CategoryId, Guid ProductId);

    private sealed record ProductWithTwoStoredImagesSeed(Guid ProductId, Guid FirstFileId, Guid SecondFileId);

    private sealed record ProductWithStoredFileSeed(Guid ProductId, Guid FileId);

    private sealed record BrandWithStoredFileSeed(Guid BrandId, Guid FileId);
}
