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
          AND storage_key LIKE 'storage/products/catalog-import/%';
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

    public const string LockProductForImageImport = """
        SELECT
            product.id AS "ProductId",
            product.name AS "ProductName"
        FROM products product
        WHERE product.external_id = @ExternalId
        FOR UPDATE;
        """;

    public const string ClearProductMainImage = """
        UPDATE product_images
        SET is_main = FALSE,
            updated_at = now()
        WHERE product_id = @ProductId
          AND is_main;
        """;

    public const string UpsertProductImage = """
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main)
        VALUES (
            @ProductId,
            @StoredFileId,
            @Alt,
            @Title,
            0,
            @IsMain)
        ON CONFLICT (product_id, stored_file_id) DO UPDATE
        SET alt = EXCLUDED.alt,
            title = EXCLUDED.title,
            sort_order = EXCLUDED.sort_order,
            is_main = CASE
                WHEN @ReplaceExistingMainImages THEN EXCLUDED.is_main
                ELSE product_images.is_main
            END;
        """;
}

public static class CatalogImportDatabaseStorage
{
    public const string ProductImageStorageKeyPrefix = "storage/products/catalog-import/";
}

public sealed record CatalogImportApplyOptions(
    bool ResetCatalog,
    bool AllowResetInCurrentEnvironment,
    bool ReplaceExistingMainImages = false,
    string? StorageRootPath = null);

public sealed record CatalogImportApplyResult(
    int CategoriesProcessed,
    int ProductsProcessed,
    int ImagesProcessed);

public sealed class CatalogImportDatabase
{
    private const string StorageRequestPathPrefix = "storage/";
    private const string ProductImageContentType = "image/png";

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
        var imageImportsByExternalId = PrepareImageImports(plan.Products, options.StorageRootPath);

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

            if (!imageImportsByExternalId.TryGetValue(product.ExternalId, out var imageImport))
            {
                continue;
            }

            var storedFileId = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertStoredFile,
                parameters: imageImport.StoredFile,
                transaction: transaction,
                cancellationToken: cancellationToken));

            await UpsertProductImageAsync(
                connection,
                transaction,
                imageImport,
                storedFileId,
                options.ResetCatalog || options.ReplaceExistingMainImages,
                cancellationToken);

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

    private static IReadOnlyDictionary<string, CatalogImportProductImageImport> PrepareImageImports(
        IReadOnlyList<CatalogProductImportRow> products,
        string? storageRootPath)
    {
        var imports = new Dictionary<string, CatalogImportProductImageImport>(StringComparer.Ordinal);
        foreach (var product in products)
        {
            if (product.Image is null)
            {
                continue;
            }

            var storedFile = CreateStoredFileParameters(product.Image);
            CopyToStorageRoot(product.Image.File, storedFile.StorageKey, storageRootPath);
            imports[product.ExternalId] = new CatalogImportProductImageImport(product.ExternalId, storedFile);
        }

        return imports;
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

    private static async Task UpsertProductImageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CatalogImportProductImageImport imageImport,
        Guid storedFileId,
        bool replaceExistingMainImages,
        CancellationToken cancellationToken)
    {
        var product = await connection.QuerySingleOrDefaultAsync<ProductImageProductRow>(new CommandDefinition(
            CatalogImportDatabaseSql.LockProductForImageImport,
            parameters: new { imageImport.ExternalId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product '{imageImport.ExternalId}' was not found while importing its image.");
        }

        if (replaceExistingMainImages)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.ClearProductMainImage,
                parameters: new { product.ProductId },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            CatalogImportDatabaseSql.UpsertProductImage,
            parameters: new
            {
                product.ProductId,
                StoredFileId = storedFileId,
                Alt = product.ProductName,
                Title = product.ProductName,
                IsMain = replaceExistingMainImages,
                ReplaceExistingMainImages = replaceExistingMainImages
            },
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
        if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Product image file must be a PNG: {image.File}");
        }

        var storageName = $"{NormalizeStorageName(image.AssetKey)}.png";

        return new CatalogImportStoredFileParameters(
            StorageKey: $"{CatalogImportDatabaseStorage.ProductImageStorageKeyPrefix}{storageName}",
            OriginalFileName: file.Name,
            ContentType: GetContentType(extension),
            SizeBytes: file.Length,
            Checksum: ComputeSha256(image.File));
    }

    private static void CopyToStorageRoot(string sourcePath, string storageKey, string? storageRootPath)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return;
        }

        var relativeStoragePath = storageKey.StartsWith(StorageRequestPathPrefix, StringComparison.Ordinal)
            ? storageKey[StorageRequestPathPrefix.Length..]
            : storageKey;
        var destinationPath = Path.Combine(
            storageRootPath,
            relativeStoragePath.Replace('/', Path.DirectorySeparatorChar));
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"Storage destination path is invalid: {destinationPath}");

        Directory.CreateDirectory(destinationDirectory);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
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
            ? ProductImageContentType
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

    private sealed record CatalogImportProductImageImport(
        string ExternalId,
        CatalogImportStoredFileParameters StoredFile);

    private sealed class ProductImageProductRow
    {
        public Guid ProductId { get; init; }

        public string ProductName { get; init; } = string.Empty;
    }
}
