using System.Security.Cryptography;
using Dapper;
using Npgsql;

namespace LineCom.CatalogImport.Core.Images;

public static class ReviewedProductImageApplySql
{
    public const string SelectProductImageState = """
        SELECT
            product.id AS "ProductId",
            product.name AS "ProductName",
            COUNT(image.id) AS "ImagesCount",
            BOOL_OR(image.is_main) AS "HasMainImage"
        FROM products product
        LEFT JOIN product_images image ON image.product_id = product.id
        WHERE product.external_id = @ExternalId
        GROUP BY product.id, product.name;
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
            'image/png',
            @SizeBytes,
            @Checksum,
            'product_image',
            'active')
        ON CONFLICT (storage_key) DO NOTHING
        RETURNING id;
        """;

    public const string SelectStoredFile = """
        SELECT id
        FROM stored_files
        WHERE storage_key = @StorageKey
          AND checksum = @Checksum
          AND content_type = 'image/png'
          AND purpose = 'product_image'
          AND status = 'active';
        """;

    public const string InsertProductImage = """
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
            @SortOrder,
            @IsMain)
        ON CONFLICT (product_id, stored_file_id) DO NOTHING;
        """;
}

public sealed record ReviewedProductImageApplyOptions(
    string ConnectionString,
    string StorageRootPath,
    bool Apply,
    bool AllowAddToProductsWithExistingImages);

public sealed record ReviewedProductImageApplyResult(
    int Planned,
    int Applied,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Errors);

public sealed record ReviewedProductImageProductState(
    Guid ProductId,
    string ProductName,
    int ImagesCount,
    bool HasMainImage);

public sealed record ReviewedProductImageApplyPlan(
    IReadOnlyList<ReviewedProductImageApplyPlanItem> Apply,
    IReadOnlyList<ReviewedProductImageSkip> Skip);

public sealed record ReviewedProductImageApplyPlanItem(
    ReviewedProductImageManifestItem Image,
    ReviewedProductImageProductState Product,
    int SortOrder,
    bool IsMain);

public sealed record ReviewedProductImageSkip(string ExternalId, string AssetKey, string Reason);

public static class ReviewedProductImageApplyPlanner
{
    public static ReviewedProductImageApplyPlan Plan(
        IReadOnlyList<ReviewedProductImageManifestItem> images,
        IReadOnlyDictionary<string, ReviewedProductImageProductState> states,
        bool allowAddToProductsWithExistingImages)
    {
        var apply = new List<ReviewedProductImageApplyPlanItem>();
        var skip = new List<ReviewedProductImageSkip>();
        foreach (var group in images.GroupBy(item => item.ExternalId, StringComparer.Ordinal))
        {
            if (!states.TryGetValue(group.Key, out var state))
            {
                foreach (var image in group)
                {
                    skip.Add(new ReviewedProductImageSkip(group.Key, image.AssetKey, "Product was not found."));
                }

                continue;
            }

            if (state.ImagesCount > 0 && !allowAddToProductsWithExistingImages)
            {
                foreach (var image in group)
                {
                    skip.Add(new ReviewedProductImageSkip(group.Key, image.AssetKey, "Product already has images."));
                }

                continue;
            }

            var ordered = group.Take(2).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var image = ordered[index];
                var isMain = state.ImagesCount == 0 && index == 0 && !state.HasMainImage;
                apply.Add(new ReviewedProductImageApplyPlanItem(image, state, index, isMain));
            }
        }

        return new ReviewedProductImageApplyPlan(apply, skip);
    }
}

public sealed class ReviewedProductImageApplyService
{
    public async Task<ReviewedProductImageApplyResult> ApplyAsync(
        string manifestPath,
        ReviewedProductImageApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        var images = ReviewedProductImageManifestReader
            .ReadAcceptedByExternalId(manifestPath)
            .SelectMany(pair => pair.Value)
            .ToArray();
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var states = new Dictionary<string, ReviewedProductImageProductState>(StringComparer.Ordinal);
        foreach (var externalId in images.Select(item => item.ExternalId).Distinct(StringComparer.Ordinal))
        {
            var state = await connection.QuerySingleOrDefaultAsync<ReviewedProductImageProductState>(new CommandDefinition(
                ReviewedProductImageApplySql.SelectProductImageState,
                parameters: new { ExternalId = externalId },
                cancellationToken: cancellationToken));
            if (state is not null)
            {
                states[externalId] = state;
            }
        }

        var plan = ReviewedProductImageApplyPlanner.Plan(
            images,
            states,
            options.AllowAddToProductsWithExistingImages);
        if (!options.Apply)
        {
            return new ReviewedProductImageApplyResult(
                plan.Apply.Count,
                0,
                plan.Skip.Select(item => $"{item.ExternalId}:{item.AssetKey}:{item.Reason}").ToArray(),
                []);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var applied = 0;
        var errors = new List<string>();
        foreach (var item in plan.Apply)
        {
            try
            {
                var storageKey = FormatStorageKey(item.Image.AssetKey, item.Image.Checksum);
                CopyToStorage(item.Image.File, options.StorageRootPath, storageKey);
                var storedFileId = await InsertOrSelectStoredFileAsync(
                    connection,
                    transaction,
                    item.Image,
                    storageKey,
                    cancellationToken);
                await connection.ExecuteAsync(new CommandDefinition(
                    ReviewedProductImageApplySql.InsertProductImage,
                    parameters: new
                    {
                        item.Product.ProductId,
                        StoredFileId = storedFileId,
                        Alt = item.Product.ProductName,
                        Title = item.Product.ProductName,
                        item.SortOrder,
                        item.IsMain
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
                applied++;
            }
            catch (Exception exception)
            {
                errors.Add($"{item.Image.ExternalId}:{item.Image.AssetKey}:{exception.Message}");
            }
        }

        if (errors.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return new ReviewedProductImageApplyResult(
            plan.Apply.Count,
            errors.Count == 0 ? applied : 0,
            plan.Skip.Select(item => $"{item.ExternalId}:{item.AssetKey}:{item.Reason}").ToArray(),
            errors);
    }

    public static string FormatStorageKey(string assetKey, string checksum)
    {
        var prefix = checksum[..Math.Min(12, checksum.Length)];
        return $"storage/products/reviewed/{assetKey}-{prefix}.png";
    }

    public static string ResolveStoragePath(string storageRootPath, string storageKey)
    {
        var relative = storageKey["storage/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(storageRootPath);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path escapes the configured root.");
        }

        return path;
    }

    private static void CopyToStorage(string sourcePath, string storageRootPath, string storageKey)
    {
        var destination = ResolveStoragePath(storageRootPath, storageKey);
        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("Storage destination path is invalid.");
        Directory.CreateDirectory(directory);
        if (File.Exists(destination) && FileMatches(destination, sourcePath))
        {
            return;
        }

        File.Copy(sourcePath, destination, overwrite: false);
    }

    private static async Task<Guid> InsertOrSelectStoredFileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ReviewedProductImageManifestItem image,
        string storageKey,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(image.File);
        var inserted = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            ReviewedProductImageApplySql.InsertStoredFile,
            parameters: new
            {
                StorageKey = storageKey,
                OriginalFileName = file.Name,
                SizeBytes = file.Length,
                image.Checksum
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
        if (inserted is not null)
        {
            return inserted.Value;
        }

        var existing = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
            ReviewedProductImageApplySql.SelectStoredFile,
            parameters: new
            {
                StorageKey = storageKey,
                image.Checksum
            },
            transaction: transaction,
            cancellationToken: cancellationToken));
        return existing ?? throw new InvalidOperationException($"Stored file '{storageKey}' could not be inserted or selected.");
    }

    private static bool FileMatches(string leftPath, string rightPath)
    {
        return string.Equals(ComputeSha256(leftPath), ComputeSha256(rightPath), StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
