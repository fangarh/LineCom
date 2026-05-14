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

    public const string CountResetImpact = """
        SELECT
            (SELECT COUNT(*) FROM categories) AS "Categories",
            (SELECT COUNT(*) FROM products) AS "Products",
            (SELECT COUNT(*) FROM product_images) AS "ProductImages",
            (
                SELECT COUNT(*)
                FROM stored_files
                WHERE purpose = 'product_image'
                  AND storage_key LIKE 'storage/products/catalog-import/%'
            ) AS "StoredProductImageFiles",
            (SELECT COUNT(*) FROM product_attribute_values) AS "ProductAttributeValues",
            (SELECT COUNT(*) FROM attribute_value_aliases) AS "AttributeValueAliases",
            (SELECT COUNT(*) FROM attribute_options) AS "AttributeOptions",
            (SELECT COUNT(*) FROM category_attributes) AS "CategoryAttributes";
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

    public const string SelectProductIdByExternalId = """
        SELECT id
        FROM products
        WHERE external_id = @ExternalId;
        """;

    public const string UpsertCategoryAttribute = """
        INSERT INTO category_attributes (
            category_id,
            name,
            code,
            type,
            is_filterable,
            is_comparable,
            is_visible_in_product,
            is_seo_important,
            is_used_in_generated_name,
            sort_order,
            is_active)
        SELECT
            category.id,
            @Name,
            @Code,
            'select',
            TRUE,
            TRUE,
            TRUE,
            @IsSeoImportant,
            @IsUsedInGeneratedName,
            @SortOrder,
            TRUE
        FROM categories category
        WHERE category.slug = @CategorySlug
        ON CONFLICT (category_id, code) DO UPDATE
        SET name = EXCLUDED.name,
            type = EXCLUDED.type,
            is_filterable = TRUE,
            is_comparable = TRUE,
            is_visible_in_product = TRUE,
            is_seo_important = EXCLUDED.is_seo_important,
            is_used_in_generated_name = EXCLUDED.is_used_in_generated_name,
            sort_order = EXCLUDED.sort_order,
            is_active = TRUE
        RETURNING id;
        """;

    public const string UpsertAttributeOption = """
        INSERT INTO attribute_options (
            attribute_id,
            value,
            slug,
            normalized_value,
            sort_order,
            is_active)
        VALUES (
            @AttributeId,
            @Value,
            @Slug,
            @NormalizedValue,
            @OptionSortOrder,
            TRUE)
        ON CONFLICT (attribute_id, slug) DO UPDATE
        SET value = EXCLUDED.value,
            normalized_value = EXCLUDED.normalized_value,
            sort_order = EXCLUDED.sort_order,
            is_active = TRUE
        RETURNING id;
        """;

    public const string UpsertProductAttributeValue = """
        INSERT INTO product_attribute_values (
            product_id,
            attribute_id,
            attribute_option_id,
            normalized_value)
        VALUES (
            @ProductId,
            @AttributeId,
            @AttributeOptionId,
            @NormalizedValue)
        ON CONFLICT (product_id, attribute_id) DO UPDATE
        SET value_text = NULL,
            value_number = NULL,
            value_boolean = NULL,
            attribute_option_id = EXCLUDED.attribute_option_id,
            normalized_value = EXCLUDED.normalized_value;
        """;

    public const string InsertStoredFile = """
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
        ON CONFLICT (storage_key) DO NOTHING
        RETURNING id;
        """;

    public const string SelectStoredFileByStorageKeyAndMetadata = """
        SELECT id
        FROM stored_files
        WHERE storage_key = @StorageKey
          AND checksum = @Checksum
          AND size_bytes = @SizeBytes
          AND content_type = @ContentType
          AND purpose = 'product_image'
          AND status = 'active';
        """;

    public const string SelectStoredFileMetadataByStorageKey = """
        SELECT
            storage_key AS "StorageKey",
            checksum AS "Checksum",
            size_bytes AS "SizeBytes",
            content_type AS "ContentType",
            purpose AS "Purpose",
            status AS "Status"
        FROM stored_files
        WHERE storage_key = @StorageKey;
        """;

    public const string SelectImportManagedStoredFileKeys = """
        SELECT storage_key
        FROM stored_files
        WHERE purpose = 'product_image'
          AND storage_key LIKE 'storage/products/catalog-import/%'
        ORDER BY storage_key;
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
    public const string StagingRootPrefix = ".staging/catalog-import/";

    private const string StorageRequestPathPrefix = "storage/";
    private const int ChecksumPrefixLength = 12;
    private const int RetryCount = 3;

    public static string FormatProductImageStorageKey(string assetKey, string checksum)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            throw new ArgumentException("Checksum is required.", nameof(checksum));
        }

        var checksumPrefixLength = Math.Min(ChecksumPrefixLength, checksum.Length);

        return $"{ProductImageStorageKeyPrefix}{NormalizeStorageName(assetKey)}-{checksum[..checksumPrefixLength]}.png";
    }

    public static string FormatStagingKey(string runId, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        var fileName = Path.GetFileName(NormalizeRelativeKey(storageKey));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Storage key file name is required.");
        }

        return $"{StagingRootPrefix}{NormalizeStorageName(runId)}/{fileName}";
    }

    public static void CopyProductImageToStorage(string sourcePath, string storageKey, string? storageRootPath)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return;
        }

        var destinationPath = ResolvePhysicalPath(storageRootPath, storageKey);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Storage destination path is invalid.");

        Directory.CreateDirectory(destinationDirectory);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase)
            && !File.Exists(destinationPath))
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);
        }
    }

    public static void StageProductImage(string sourcePath, string stagingKey, string? storageRootPath)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return;
        }

        var destinationPath = ResolvePhysicalPath(storageRootPath, stagingKey);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Storage staging path is invalid.");

        Directory.CreateDirectory(destinationDirectory);
        Retry(() => File.Copy(sourcePath, destinationPath, overwrite: false));
    }

    public static void StageProductImages(
        IReadOnlyList<CatalogImportStorageManifestItem> manifest,
        string? storageRootPath)
    {
        foreach (var item in manifest)
        {
            StageProductImage(item.SourcePath, item.StagingKey, storageRootPath);
            item.Staged = !string.IsNullOrWhiteSpace(storageRootPath);
        }
    }

    public static CatalogImportStorageOperationFailure? PromoteStagedProductImage(
        string stagingKey,
        string storageKey,
        string? storageRootPath,
        string expectedChecksum,
        long expectedSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return null;
        }

        var stagingPath = ResolvePhysicalPath(storageRootPath, stagingKey);
        var destinationPath = ResolvePhysicalPath(storageRootPath, storageKey);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("Storage destination path is invalid.");

        if (!File.Exists(stagingPath))
        {
            return new CatalogImportStorageOperationFailure(
                storageKey,
                $"Staged file '{stagingKey}' was not found.");
        }

        Directory.CreateDirectory(destinationDirectory);
        if (File.Exists(destinationPath))
        {
            if (FileMatches(destinationPath, expectedChecksum, expectedSizeBytes))
            {
                var cleanupFailure = DeleteRelativePath(stagingKey, storageRootPath);
                return cleanupFailure is null
                    ? null
                    : new CatalogImportStorageOperationFailure(storageKey, cleanupFailure.Error);
            }

            return new CatalogImportStorageOperationFailure(
                storageKey,
                "Destination file already exists with different content.");
        }

        return RetryFailure(
            storageKey,
            () => File.Move(stagingPath, destinationPath, overwrite: false));
    }

    public static CatalogImportStorageOperationFailure? DeleteRelativePath(string relativeKey, string? storageRootPath)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return null;
        }

        var physicalPath = ResolvePhysicalPath(storageRootPath, relativeKey);
        return RetryFailure(
            relativeKey,
            () =>
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            });
    }

    public static IReadOnlyList<string> FindOldStagingLeftovers(string? storageRootPath, string currentRunId)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return [];
        }

        var stagingRoot = ResolvePhysicalPath(storageRootPath, StagingRootPrefix.TrimEnd('/'));
        if (!Directory.Exists(stagingRoot))
        {
            return [];
        }

        var currentRunKey = $"{StagingRootPrefix}{currentRunId}";
        var currentRunPrefix = $"{currentRunKey}/";
        return Directory
            .EnumerateFileSystemEntries(stagingRoot)
            .Select(path => ToStorageRelativeKey(storageRootPath, path))
            .Where(key => !string.Equals(key, currentRunKey, StringComparison.Ordinal)
                && !key.StartsWith(currentRunPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> FindUntrackedProductImageFiles(
        string? storageRootPath,
        IReadOnlySet<string> trackedStorageKeys)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return [];
        }

        var productImageRoot = ResolvePhysicalPath(storageRootPath, ProductImageStorageKeyPrefix.TrimEnd('/'));
        if (!Directory.Exists(productImageRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(productImageRoot, "*", SearchOption.AllDirectories)
            .Select(path => $"{StorageRequestPathPrefix}{ToStorageRelativeKey(storageRootPath, path)}")
            .Where(storageKey => !trackedStorageKeys.Contains(storageKey))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static string ResolvePhysicalPath(string storageRootPath, string relativeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRootPath);
        var normalizedKey = NormalizeRelativeKey(relativeKey);
        if (normalizedKey.StartsWith(StorageRequestPathPrefix, StringComparison.Ordinal))
        {
            normalizedKey = normalizedKey[StorageRequestPathPrefix.Length..];
        }

        var rootFullPath = Path.GetFullPath(storageRootPath);
        var pathParts = normalizedKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
        {
            throw new InvalidOperationException("Storage path is invalid.");
        }

        var physicalPath = Path.GetFullPath(Path.Combine([rootFullPath, .. pathParts]));
        var rootWithSeparator = rootFullPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootFullPath
            : rootFullPath + Path.DirectorySeparatorChar;

        if (!physicalPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path escapes the configured root.");
        }

        return physicalPath;
    }

    private static string NormalizeRelativeKey(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
        {
            throw new InvalidOperationException("Storage key is required.");
        }

        var raw = relativeKey.Replace('\\', '/');
        if (raw.StartsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(raw))
        {
            throw new InvalidOperationException("Storage key is invalid.");
        }

        var normalized = raw.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)
            || Path.IsPathRooted(normalized)
            || normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(part => part is "." or ".."))
        {
            throw new InvalidOperationException("Storage key is invalid.");
        }

        return normalized;
    }

    private static string ToStorageRelativeKey(string storageRootPath, string physicalPath)
    {
        return Path.GetRelativePath(Path.GetFullPath(storageRootPath), Path.GetFullPath(physicalPath))
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Trim('/');
    }

    private static string NormalizeStorageName(string value)
    {
        var normalized = value
            .Replace("\\", "-", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal)
            .Trim(' ', '.', '-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Product image asset key is required.");
        }

        return normalized;
    }

    private static bool FileMatches(string path, string checksum, long sizeBytes)
    {
        var file = new FileInfo(path);
        return file.Length == sizeBytes
            && string.Equals(ComputeSha256(path), checksum, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static CatalogImportStorageOperationFailure? RetryFailure(string key, Action action)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= RetryCount; attempt++)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception exception) when (attempt < RetryCount)
            {
                lastException = exception;
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        return new CatalogImportStorageOperationFailure(
            key,
            lastException?.Message ?? "Storage operation failed.");
    }

    private static void Retry(Action action)
    {
        var failure = RetryFailure("storage operation", action);
        if (failure is not null)
        {
            throw new IOException(failure.Error);
        }
    }
}

public sealed record CatalogImportApplyOptions(
    bool ResetCatalog,
    bool AllowResetInCurrentEnvironment,
    bool ReplaceExistingMainImages = false,
    string? StorageRootPath = null);

public sealed record CatalogImportApplyResult(
    int CategoriesProcessed,
    int ProductsProcessed,
    int ImagesProcessed,
    CatalogImportResetImpact? ResetImpact = null,
    CatalogImportApplyStorageResult? Storage = null,
    CatalogImportResetStorageCleanupResult? ResetStorageCleanup = null);

public sealed record CatalogImportResetImpact(
    long Categories,
    long Products,
    long ProductImages,
    long StoredProductImageFiles,
    long ProductAttributeValues,
    long AttributeValueAliases,
    long AttributeOptions,
    long CategoryAttributes);

public sealed record CatalogImportApplyStorageResult(
    string RunId,
    int StagedFiles,
    int PromotedFiles,
    IReadOnlyList<CatalogImportStorageOperationFailure> PromotionFailures,
    IReadOnlyList<CatalogImportStorageOperationFailure> CleanupFailures,
    IReadOnlyList<string> OldStagingLeftovers);

public sealed record CatalogImportResetStorageCleanupResult(
    int SelectedFiles,
    int DeletedFiles,
    IReadOnlyList<CatalogImportStorageOperationFailure> Failures,
    IReadOnlyList<string> UntrackedLeftovers);

public sealed record CatalogImportStorageOperationFailure(string Key, string Error);

public sealed record CatalogImportStorageConflict(
    string StorageKey,
    string SourceAssetKey,
    string OriginalFileName,
    string ExpectedChecksum,
    long ExpectedSizeBytes,
    string ExpectedContentType,
    string ExistingChecksum,
    long ExistingSizeBytes,
    string ExistingContentType);

public sealed class CatalogImportStorageConflictException : InvalidOperationException
{
    public CatalogImportStorageConflictException(CatalogImportStorageConflict conflict)
        : base(
            "Stored product image file conflict: "
            + $"storageKey '{conflict.StorageKey}', source '{conflict.SourceAssetKey}', "
            + $"file '{conflict.OriginalFileName}', expected checksum '{conflict.ExpectedChecksum}' "
            + $"size {conflict.ExpectedSizeBytes} content-type '{conflict.ExpectedContentType}', "
            + $"existing checksum '{conflict.ExistingChecksum}' size {conflict.ExistingSizeBytes} "
            + $"content-type '{conflict.ExistingContentType}'.")
    {
        Conflict = conflict;
    }

    public CatalogImportStorageConflict Conflict { get; }
}

public sealed class CatalogImportStorageManifestItem
{
    public CatalogImportStorageManifestItem(
        string sourceAssetKey,
        string originalFileName,
        string sourcePath,
        string stagingKey,
        string storageKey,
        string checksum,
        long sizeBytes,
        string contentType)
    {
        SourceAssetKey = sourceAssetKey;
        OriginalFileName = originalFileName;
        SourcePath = sourcePath;
        StagingKey = stagingKey;
        StorageKey = storageKey;
        Checksum = checksum;
        SizeBytes = sizeBytes;
        ContentType = contentType;
    }

    public string SourceAssetKey { get; }

    public string OriginalFileName { get; }

    public string SourcePath { get; }

    public string StagingKey { get; }

    public string StorageKey { get; }

    public string Checksum { get; }

    public long SizeBytes { get; }

    public string ContentType { get; }

    public bool Staged { get; set; }

    public bool Promoted { get; set; }
}

public sealed record CatalogImportStorageRun(
    string RunId,
    string? StorageRootPath,
    IReadOnlyList<CatalogImportStorageManifestItem> Manifest,
    IReadOnlyList<string> OldStagingLeftovers);


public sealed class CatalogImportDatabase
{
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
        var imageImportsByExternalId = PrepareImageImports(plan.Products);
        var storageRun = CreateStorageRun(imageImportsByExternalId.Values, options.StorageRootPath);
        IReadOnlyList<string> resetStorageKeys = [];

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            await EnsureNoStoredFileConflictsAsync(connection, storageRun.Manifest, cancellationToken);
            CatalogImportDatabaseStorage.StageProductImages(storageRun.Manifest, options.StorageRootPath);
        }
        catch
        {
            CleanupCurrentRun(storageRun.Manifest, options.StorageRootPath);
            throw;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var committed = false;

        CatalogImportResetImpact? resetImpact = null;
        try
        {
            if (options.ResetCatalog)
            {
                resetStorageKeys = await SelectImportManagedStoredFileKeysAsync(connection, transaction, cancellationToken);
                resetImpact = await ResetCatalogAsync(connection, transaction, cancellationToken);
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

                await UpsertProductAttributesAsync(connection, transaction, product, cancellationToken);

                if (!imageImportsByExternalId.TryGetValue(product.ExternalId, out var imageImport))
                {
                    continue;
                }

                var insertedStoredFileId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                    CatalogImportDatabaseSql.InsertStoredFile,
                    parameters: imageImport.StoredFile,
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                var storedFileId = insertedStoredFileId ?? await SelectExistingStoredFileIdAsync(
                    connection,
                    transaction,
                    imageImport.StoredFile,
                    cancellationToken);

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
            committed = true;

            var storageResult = PromoteStagedImages(storageRun, options.StorageRootPath);
            var resetStorageCleanup = options.ResetCatalog
                ? CleanupResetStorage(resetStorageKeys, storageRun.Manifest, options.StorageRootPath)
                : null;

            return new CatalogImportApplyResult(
                plan.Categories.Count,
                plan.Products.Count,
                imagesProcessed,
                resetImpact,
                storageResult,
                resetStorageCleanup);
        }
        catch
        {
            if (!committed)
            {
                CleanupCurrentRun(storageRun.Manifest, options.StorageRootPath);
            }

            throw;
        }
    }

    private static async Task UpsertProductAttributesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CatalogProductImportRow product,
        CancellationToken cancellationToken)
    {
        if (product.Attributes.Count == 0)
        {
            return;
        }

        var productId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            CatalogImportDatabaseSql.SelectProductIdByExternalId,
            parameters: new { product.ExternalId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (productId is null)
        {
            throw new InvalidOperationException($"Product '{product.ExternalId}' was not found while importing attributes.");
        }

        foreach (var attribute in product.Attributes)
        {
            var attributeId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertCategoryAttribute,
                parameters: new
                {
                    product.CategorySlug,
                    attribute.Name,
                    attribute.Code,
                    attribute.SortOrder,
                    attribute.IsSeoImportant,
                    attribute.IsUsedInGeneratedName
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (attributeId is null)
            {
                throw new InvalidOperationException(
                    $"Attribute '{attribute.Code}' was not imported because category '{product.CategorySlug}' was not found.");
            }

            var optionId = await connection.QuerySingleAsync<Guid>(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertAttributeOption,
                parameters: new
                {
                    AttributeId = attributeId.Value,
                    attribute.Value,
                    attribute.Slug,
                    attribute.NormalizedValue,
                    attribute.OptionSortOrder
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertProductAttributeValue,
                parameters: new
                {
                    ProductId = productId.Value,
                    AttributeId = attributeId.Value,
                    AttributeOptionId = optionId,
                    attribute.NormalizedValue
                },
                transaction: transaction,
                cancellationToken: cancellationToken));
        }
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
        IReadOnlyList<CatalogProductImportRow> products)
    {
        var imports = new Dictionary<string, CatalogImportProductImageImport>(StringComparer.Ordinal);
        foreach (var product in products)
        {
            if (product.Image is null)
            {
                continue;
            }

            var storedFile = CreateStoredFileParameters(product.Image);
            imports[product.ExternalId] = new CatalogImportProductImageImport(
                product.ExternalId,
                product.Image.AssetKey,
                product.Image.File,
                storedFile);
        }

        return imports;
    }

    private static async Task<CatalogImportResetImpact> ResetCatalogAsync(
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

        var resetImpact = await connection.QuerySingleAsync<CatalogImportResetImpact>(new CommandDefinition(
            CatalogImportDatabaseSql.CountResetImpact,
            transaction: transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            CatalogImportDatabaseSql.ResetCatalog,
            transaction: transaction,
            cancellationToken: cancellationToken));

        return resetImpact;
    }

    private static async Task<IReadOnlyList<string>> SelectImportManagedStoredFileKeysAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var storageKeys = await connection.QueryAsync<string>(new CommandDefinition(
            CatalogImportDatabaseSql.SelectImportManagedStoredFileKeys,
            transaction: transaction,
            cancellationToken: cancellationToken));

        return storageKeys.AsList();
    }

    private static CatalogImportStorageRun CreateStorageRun(
        IEnumerable<CatalogImportProductImageImport> imageImports,
        string? storageRootPath)
    {
        var runId = Guid.NewGuid().ToString("N");
        var manifest = imageImports
            .Select(imageImport => new CatalogImportStorageManifestItem(
                imageImport.AssetKey,
                imageImport.StoredFile.OriginalFileName,
                imageImport.SourcePath,
                CatalogImportDatabaseStorage.FormatStagingKey(runId, imageImport.StoredFile.StorageKey),
                imageImport.StoredFile.StorageKey,
                imageImport.StoredFile.Checksum,
                imageImport.StoredFile.SizeBytes,
                imageImport.StoredFile.ContentType))
            .ToArray();

        return new CatalogImportStorageRun(
            runId,
            storageRootPath,
            manifest,
            CatalogImportDatabaseStorage.FindOldStagingLeftovers(storageRootPath, runId));
    }

    private static async Task EnsureNoStoredFileConflictsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<CatalogImportStorageManifestItem> manifest,
        CancellationToken cancellationToken)
    {
        foreach (var item in manifest)
        {
            var existing = await connection.QuerySingleOrDefaultAsync<StoredFileMetadataRow>(new CommandDefinition(
                CatalogImportDatabaseSql.SelectStoredFileMetadataByStorageKey,
                parameters: new { item.StorageKey },
                cancellationToken: cancellationToken));

            if (existing is null)
            {
                continue;
            }

            var matches = string.Equals(existing.Checksum, item.Checksum, StringComparison.OrdinalIgnoreCase)
                && existing.SizeBytes == item.SizeBytes
                && string.Equals(existing.ContentType, item.ContentType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Purpose, "product_image", StringComparison.Ordinal)
                && string.Equals(existing.Status, "active", StringComparison.Ordinal);

            if (!matches)
            {
                throw new CatalogImportStorageConflictException(new CatalogImportStorageConflict(
                    item.StorageKey,
                    item.SourceAssetKey,
                    item.OriginalFileName,
                    item.Checksum,
                    item.SizeBytes,
                    item.ContentType,
                    existing.Checksum,
                    existing.SizeBytes,
                    existing.ContentType));
            }
        }
    }

    private static CatalogImportApplyStorageResult PromoteStagedImages(
        CatalogImportStorageRun storageRun,
        string? storageRootPath)
    {
        var promotionFailures = new List<CatalogImportStorageOperationFailure>();
        var cleanupFailures = new List<CatalogImportStorageOperationFailure>();
        var promotedFiles = 0;

        foreach (var item in storageRun.Manifest)
        {
            var failure = CatalogImportDatabaseStorage.PromoteStagedProductImage(
                item.StagingKey,
                item.StorageKey,
                storageRootPath,
                item.Checksum,
                item.SizeBytes);

            if (failure is null)
            {
                item.Promoted = true;
                promotedFiles++;
                continue;
            }

            promotionFailures.Add(failure);
            var cleanupFailure = CatalogImportDatabaseStorage.DeleteRelativePath(item.StagingKey, storageRootPath);
            if (cleanupFailure is not null)
            {
                cleanupFailures.Add(cleanupFailure);
            }
        }

        CleanupEmptyStagingDirectory(storageRun, storageRootPath, cleanupFailures);

        return new CatalogImportApplyStorageResult(
            storageRun.RunId,
            storageRun.Manifest.Count,
            promotedFiles,
            promotionFailures,
            cleanupFailures,
            storageRun.OldStagingLeftovers);
    }

    private static IReadOnlyList<CatalogImportStorageOperationFailure> CleanupCurrentRun(
        IReadOnlyList<CatalogImportStorageManifestItem> manifest,
        string? storageRootPath)
    {
        var failures = new List<CatalogImportStorageOperationFailure>();
        foreach (var item in manifest)
        {
            var stagingFailure = CatalogImportDatabaseStorage.DeleteRelativePath(item.StagingKey, storageRootPath);
            if (stagingFailure is not null)
            {
                failures.Add(stagingFailure);
            }

            if (item.Promoted)
            {
                var promotedFailure = CatalogImportDatabaseStorage.DeleteRelativePath(item.StorageKey, storageRootPath);
                if (promotedFailure is not null)
                {
                    failures.Add(promotedFailure);
                }
            }
        }

        return failures;
    }

    private static CatalogImportResetStorageCleanupResult CleanupResetStorage(
        IReadOnlyList<string> resetStorageKeys,
        IReadOnlyList<CatalogImportStorageManifestItem> currentRunManifest,
        string? storageRootPath)
    {
        var currentRunKeys = currentRunManifest
            .Select(item => item.StorageKey)
            .ToHashSet(StringComparer.Ordinal);
        var deleteKeys = resetStorageKeys
            .Where(storageKey => !currentRunKeys.Contains(storageKey))
            .ToArray();
        var failures = new List<CatalogImportStorageOperationFailure>();
        var deletedFiles = 0;

        foreach (var storageKey in deleteKeys)
        {
            var failure = CatalogImportDatabaseStorage.DeleteRelativePath(storageKey, storageRootPath);
            if (failure is null)
            {
                deletedFiles++;
            }
            else
            {
                failures.Add(failure);
            }
        }

        var trackedKeys = resetStorageKeys
            .Concat(currentRunKeys)
            .ToHashSet(StringComparer.Ordinal);
        var untrackedLeftovers = CatalogImportDatabaseStorage.FindUntrackedProductImageFiles(storageRootPath, trackedKeys);

        return new CatalogImportResetStorageCleanupResult(
            resetStorageKeys.Count,
            deletedFiles,
            failures,
            untrackedLeftovers);
    }

    private static void CleanupEmptyStagingDirectory(
        CatalogImportStorageRun storageRun,
        string? storageRootPath,
        List<CatalogImportStorageOperationFailure> cleanupFailures)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            return;
        }

        try
        {
            var runDirectory = CatalogImportDatabaseStorage.ResolvePhysicalPath(
                storageRootPath,
                $"{CatalogImportDatabaseStorage.StagingRootPrefix}{storageRun.RunId}");
            if (Directory.Exists(runDirectory) && !Directory.EnumerateFileSystemEntries(runDirectory).Any())
            {
                Directory.Delete(runDirectory);
            }
        }
        catch (Exception exception)
        {
            cleanupFailures.Add(new CatalogImportStorageOperationFailure(
                $"{CatalogImportDatabaseStorage.StagingRootPrefix}{storageRun.RunId}",
                exception.Message));
        }
    }

    private static async Task<Guid> SelectExistingStoredFileIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CatalogImportStoredFileParameters storedFile,
        CancellationToken cancellationToken)
    {
        var storedFileId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata,
            parameters: storedFile,
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (storedFileId is null)
        {
            throw new InvalidOperationException(
                $"Stored product image file '{storedFile.StorageKey}' already exists with different metadata.");
        }

        return storedFileId.Value;
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

        var checksum = ComputeSha256(image.File);

        return new CatalogImportStoredFileParameters(
            StorageKey: CatalogImportDatabaseStorage.FormatProductImageStorageKey(image.AssetKey, checksum),
            OriginalFileName: file.Name,
            ContentType: GetContentType(extension),
            SizeBytes: file.Length,
            Checksum: checksum);
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
        string AssetKey,
        string SourcePath,
        CatalogImportStoredFileParameters StoredFile);

    private sealed class StoredFileMetadataRow
    {
        public string StorageKey { get; init; } = string.Empty;

        public string Checksum { get; init; } = string.Empty;

        public long SizeBytes { get; init; }

        public string ContentType { get; init; } = string.Empty;

        public string Purpose { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;
    }

    private sealed class ProductImageProductRow
    {
        public Guid ProductId { get; init; }

        public string ProductName { get; init; } = string.Empty;
    }
}
