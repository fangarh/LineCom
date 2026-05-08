using Dapper;
using LineCom.CatalogImport.Core.Planning;
using Npgsql;
using System.Security.Cryptography;

namespace LineCom.CatalogImport.Core.Database;

public static class CatalogImportDatabaseSql
{
    public const string CountProtectedProductReferences = """
        SELECT COUNT(*)
        FROM request_items item
        JOIN products product ON product.id = item.product_id;
        """;

    public const string ResetCatalog = """
        DELETE FROM product_images;
        DELETE FROM product_attribute_values;
        DELETE FROM attribute_value_aliases;
        DELETE FROM attribute_options;
        DELETE FROM category_attributes;
        DELETE FROM products;
        UPDATE categories SET parent_id = NULL WHERE parent_id IS NOT NULL;
        DELETE FROM categories;
        DELETE FROM stored_files
        WHERE purpose = 'product_image'
          AND storage_key LIKE 'catalog-import/products/%';
        """;

    public const string UpsertCategory = """
        INSERT INTO categories (
            slug,
            name,
            sort_order,
            is_active,
            is_visible_in_menu)
        VALUES (
            @Slug,
            @Name,
            @SortOrder,
            @IsActive,
            @IsVisibleInMenu)
        ON CONFLICT (slug) DO UPDATE
        SET name = EXCLUDED.name,
            sort_order = EXCLUDED.sort_order,
            is_active = EXCLUDED.is_active,
            is_visible_in_menu = EXCLUDED.is_visible_in_menu;
        """;

    public const string UpsertProduct = """
        INSERT INTO products (
            primary_category_id,
            name,
            slug,
            external_id,
            availability_status,
            sale_unit,
            unit_quantity,
            publish_status,
            sort_order)
        SELECT
            category.id,
            @Name,
            @Slug,
            @ExternalId,
            @AvailabilityStatus,
            @SaleUnit,
            @UnitQuantity,
            @PublishStatus,
            @SortOrder
        FROM categories category
        WHERE category.slug = @CategorySlug
        ON CONFLICT (external_id) WHERE external_id IS NOT NULL DO UPDATE
        SET primary_category_id = EXCLUDED.primary_category_id,
            name = EXCLUDED.name,
            slug = EXCLUDED.slug,
            availability_status = EXCLUDED.availability_status,
            sale_unit = EXCLUDED.sale_unit,
            unit_quantity = EXCLUDED.unit_quantity,
            publish_status = EXCLUDED.publish_status,
            sort_order = EXCLUDED.sort_order;
        """;

    public const string UpsertStoredFile = """
        INSERT INTO stored_files (
            storage_key,
            original_file_name,
            content_type,
            size_bytes,
            checksum,
            purpose,
            status)
        VALUES (
            @StorageKey,
            @OriginalFileName,
            @ContentType,
            @SizeBytes,
            @Checksum,
            'product_image',
            'active')
        ON CONFLICT (storage_key) DO UPDATE
        SET original_file_name = EXCLUDED.original_file_name,
            content_type = EXCLUDED.content_type,
            size_bytes = EXCLUDED.size_bytes,
            checksum = EXCLUDED.checksum,
            purpose = EXCLUDED.purpose,
            status = EXCLUDED.status
        RETURNING id;
        """;

    public const string UpsertProductImage = """
        WITH product_row AS (
            SELECT product.id, product.name
            FROM products product
            WHERE product.external_id = @ExternalId
        ),
        clear_existing_main AS (
            UPDATE product_images
            SET is_main = FALSE,
                updated_at = now()
            FROM product_row product
            WHERE product_images.product_id = product.id
              AND product_images.stored_file_id <> @StoredFileId
              AND product_images.is_main
            RETURNING product_images.id
        )
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main)
        SELECT
            product.id,
            @StoredFileId,
            product.name,
            product.name,
            0,
            TRUE
        FROM product_row product
        ON CONFLICT (product_id, stored_file_id) DO UPDATE
        SET alt = EXCLUDED.alt,
            title = EXCLUDED.title,
            sort_order = EXCLUDED.sort_order,
            is_main = EXCLUDED.is_main;
        """;
}

public sealed record CatalogImportApplyOptions(
    bool ResetCatalog,
    bool AllowResetInCurrentEnvironment);

public sealed record CatalogImportApplyResult(
    int CategoriesProcessed,
    int ProductsProcessed,
    int ImagesProcessed);

public sealed class CatalogImportDatabase
{
    private const string ProductImageStoragePrefix = "catalog-import/products/";

    private readonly string _connectionString;

    public CatalogImportDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<CatalogImportApplyResult> ApplyAsync(
        CatalogImportPlan plan,
        CatalogImportApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);

        ValidateResetOptions(options);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (options.ResetCatalog)
        {
            await ResetCatalogAsync(connection, transaction, cancellationToken);
        }

        foreach (var category in plan.Categories)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertCategory,
                parameters: category,
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        var imagesProcessed = 0;
        foreach (var product in plan.Products)
        {
            var productsAffected = await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertProduct,
                parameters: product,
                transaction: transaction,
                cancellationToken: cancellationToken));
            if (productsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Product '{product.ExternalId}' was not imported because category '{product.CategorySlug}' was not found.");
            }

            if (product.Image is null)
            {
                continue;
            }

            var storedFile = CreateStoredFileParameters(product.Image);
            var storedFileId = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertStoredFile,
                parameters: storedFile,
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertProductImage,
                parameters: new { product.ExternalId, StoredFileId = storedFileId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            imagesProcessed++;
        }

        await transaction.CommitAsync(cancellationToken);

        return new CatalogImportApplyResult(
            plan.Categories.Count,
            plan.Products.Count,
            imagesProcessed);
    }

    private static void ValidateResetOptions(CatalogImportApplyOptions options)
    {
        if (options.ResetCatalog && !options.AllowResetInCurrentEnvironment)
        {
            throw new InvalidOperationException(
                "Catalog reset is allowed only for explicitly approved dev/QA environments.");
        }
    }

    private static async Task ResetCatalogAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var protectedReferences = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            CatalogImportDatabaseSql.CountProtectedProductReferences,
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (protectedReferences > 0)
        {
            throw new InvalidOperationException(
                $"Catalog reset refused because {protectedReferences} request item(s) reference products.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            CatalogImportDatabaseSql.ResetCatalog,
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static CatalogImportStoredFileParameters CreateStoredFileParameters(CatalogProductImageImportRow image)
    {
        if (string.IsNullOrWhiteSpace(image.File))
        {
            throw new InvalidOperationException($"Product image '{image.AssetKey}' does not define a file path.");
        }

        if (!File.Exists(image.File))
        {
            throw new InvalidOperationException($"Product image file was not found: {image.File}");
        }

        var file = new FileInfo(image.File);
        var extension = Path.GetExtension(file.Name);
        var storageName = string.IsNullOrWhiteSpace(extension)
            ? NormalizeStorageName(image.AssetKey)
            : $"{NormalizeStorageName(image.AssetKey)}{extension.ToLowerInvariant()}";

        return new CatalogImportStoredFileParameters(
            StorageKey: $"{ProductImageStoragePrefix}{storageName}",
            OriginalFileName: file.Name,
            ContentType: GetContentType(extension),
            SizeBytes: file.Length,
            Checksum: ComputeSha256(image.File));
    }

    private static string NormalizeStorageName(string value)
    {
        var normalized = value
            .Replace("\\", "-", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Trim(' ', '.', '-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Product image asset key is required.");
        }

        return normalized;
    }

    private static string GetContentType(string extension)
    {
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "application/octet-stream";
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record CatalogImportStoredFileParameters(
        string StorageKey,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        string Checksum);
}
