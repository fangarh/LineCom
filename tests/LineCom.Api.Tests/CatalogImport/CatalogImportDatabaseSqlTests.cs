using LineCom.CatalogImport.Core.Database;
using LineCom.CatalogImport.Core.Planning;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportDatabaseSqlTests
{
    [Fact]
    public void UpsertCategory_UsesSlugIdentity()
    {
        Assert.Contains("INSERT INTO categories", CatalogImportDatabaseSql.UpsertCategory);
        Assert.Contains("ON CONFLICT (slug) DO UPDATE", CatalogImportDatabaseSql.UpsertCategory);
        Assert.Contains("is_visible_in_menu = EXCLUDED.is_visible_in_menu", CatalogImportDatabaseSql.UpsertCategory);
    }

    [Fact]
    public void UpsertProduct_UsesExternalIdIdentityAndDoesNotImportCommerceFields()
    {
        Assert.Contains("INSERT INTO products", CatalogImportDatabaseSql.UpsertProduct);
        Assert.Contains("FROM categories category", CatalogImportDatabaseSql.UpsertProduct);
        Assert.Contains("WHERE category.slug = @CategorySlug", CatalogImportDatabaseSql.UpsertProduct);
        Assert.Contains("ON CONFLICT (external_id) WHERE external_id IS NOT NULL DO UPDATE", CatalogImportDatabaseSql.UpsertProduct);
        Assert.DoesNotContain("price", CatalogImportDatabaseSql.UpsertProduct, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock", CatalogImportDatabaseSql.UpsertProduct, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImageSql_RegistersProductImageFilesAndConnectsProductImages()
    {
        Assert.Contains("INSERT INTO stored_files", CatalogImportDatabaseSql.InsertStoredFile);
        Assert.Contains("'product_image'", CatalogImportDatabaseSql.InsertStoredFile);
        Assert.Contains("'active'", CatalogImportDatabaseSql.InsertStoredFile);
        Assert.Contains("ON CONFLICT (storage_key) DO NOTHING", CatalogImportDatabaseSql.InsertStoredFile);
        Assert.Contains("RETURNING id", CatalogImportDatabaseSql.InsertStoredFile);
        Assert.Contains("FROM stored_files", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);
        Assert.Contains("checksum = @Checksum", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);
        Assert.Contains("size_bytes = @SizeBytes", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);
        Assert.Contains("content_type = @ContentType", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);
        Assert.Contains("purpose = 'product_image'", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);
        Assert.Contains("status = 'active'", CatalogImportDatabaseSql.SelectStoredFileByStorageKeyAndMetadata);

        Assert.Contains("UPDATE product_images", CatalogImportDatabaseSql.ClearProductMainImage);
        Assert.Contains("SET is_main = FALSE", CatalogImportDatabaseSql.ClearProductMainImage);
        Assert.Contains("INSERT INTO product_images", CatalogImportDatabaseSql.UpsertProductImage);
        Assert.Contains("WHERE product.external_id = @ExternalId", CatalogImportDatabaseSql.LockProductForImageImport);
        Assert.Contains("ON CONFLICT (product_id, stored_file_id) DO UPDATE", CatalogImportDatabaseSql.UpsertProductImage);
        Assert.Contains("WHEN @ReplaceExistingMainImages THEN EXCLUDED.is_main", CatalogImportDatabaseSql.UpsertProductImage);
        Assert.Contains("ELSE product_images.is_main", CatalogImportDatabaseSql.UpsertProductImage);
    }

    [Fact]
    public void StoredFileSql_DoesNotRewriteMetadataForExistingStorageKey()
    {
        Assert.DoesNotContain("DO UPDATE", CatalogImportDatabaseSql.InsertStoredFile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SET original_file_name", CatalogImportDatabaseSql.InsertStoredFile, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyFlow_CopiesImagesOnlyAfterResetSafetyChecks()
    {
        var source = ReadCatalogImportDatabaseSource();
        var prepareImageImports = ExtractMethodBody(source, "PrepareImageImports");
        var applyAsync = ExtractMethodBody(source, "ApplyAsync");

        Assert.DoesNotContain("CopyProductImageToStorage", prepareImageImports);
        Assert.Contains("CopyPreparedImagesToStorage", applyAsync);
        Assert.True(
            applyAsync.IndexOf("ResetCatalogAsync", StringComparison.Ordinal)
                < applyAsync.IndexOf("CopyPreparedImagesToStorage", StringComparison.Ordinal));
        Assert.True(
            applyAsync.IndexOf("CopyPreparedImagesToStorage", StringComparison.Ordinal)
                < applyAsync.IndexOf("CatalogImportDatabaseSql.InsertStoredFile", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductImageReplacementSql_UsesExplicitOrderedCommandsWithoutDataModifyingCte()
    {
        Assert.Contains("FOR UPDATE", CatalogImportDatabaseSql.LockProductForImageImport);
        Assert.Contains("WHERE product.external_id = @ExternalId", CatalogImportDatabaseSql.LockProductForImageImport);

        Assert.StartsWith("UPDATE product_images", CatalogImportDatabaseSql.ClearProductMainImage.TrimStart());
        Assert.Contains("WHERE product_id = @ProductId", CatalogImportDatabaseSql.ClearProductMainImage);
        Assert.Contains("AND is_main", CatalogImportDatabaseSql.ClearProductMainImage);

        Assert.StartsWith("INSERT INTO product_images", CatalogImportDatabaseSql.UpsertProductImage.TrimStart());
        Assert.DoesNotContain("WITH", CatalogImportDatabaseSql.UpsertProductImage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE product_images", CatalogImportDatabaseSql.UpsertProductImage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@IsMain", CatalogImportDatabaseSql.UpsertProductImage);
    }

    [Fact]
    public void ProductImageReplacementPolicy_DefaultsToNotReplacingExistingMainImages()
    {
        var options = new CatalogImportApplyOptions(ResetCatalog: false, AllowResetInCurrentEnvironment: false);

        Assert.False(options.ReplaceExistingMainImages);
    }

    [Fact]
    public void StorageKeyPrefix_UsesLocalStoragePublicUrlContract()
    {
        Assert.Equal("storage/products/catalog-import/", CatalogImportDatabaseStorage.ProductImageStorageKeyPrefix);
        Assert.Contains("storage_key LIKE 'storage/products/catalog-import/%'", CatalogImportDatabaseSql.ResetCatalog);
        Assert.DoesNotContain("catalog-import/products/%", CatalogImportDatabaseSql.ResetCatalog);
    }

    [Fact]
    public void ProductImageStorageKey_IncludesChecksumPrefixAndKeepsLocalStorageContract()
    {
        var storageKey = CatalogImportDatabaseStorage.FormatProductImageStorageKey(
            "asset/key one",
            "abcdef1234567890fedcba0987654321abcdef1234567890fedcba0987654321");

        Assert.Equal("storage/products/catalog-import/asset-key-one-abcdef123456.png", storageKey);
    }

    [Fact]
    public void CopyProductImageToStorage_DoesNotOverwriteExistingPublicFile()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "source.png");
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var storageKey = "storage/products/catalog-import/asset-abcdef123456.png";
        var existingPath = Path.Combine(storageRootPath, "products", "catalog-import", "asset-abcdef123456.png");
        Directory.CreateDirectory(Path.GetDirectoryName(existingPath)!);
        File.WriteAllText(sourcePath, "new-bytes");
        File.WriteAllText(existingPath, "existing-bytes");

        CatalogImportDatabaseStorage.CopyProductImageToStorage(sourcePath, storageKey, storageRootPath);

        Assert.Equal("existing-bytes", File.ReadAllText(existingPath));
    }

    [Fact]
    public void ResetSql_ChecksProtectedRequestItemsAndDoesNotDeleteRequests()
    {
        Assert.Contains("FROM request_items item", CatalogImportDatabaseSql.CountProtectedProductReferences);
        Assert.Contains("JOIN products product ON product.id = item.product_id", CatalogImportDatabaseSql.CountProtectedProductReferences);

        Assert.Contains("DELETE FROM product_images", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM product_attribute_values", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM attribute_value_aliases", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM attribute_options", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM category_attributes", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM products", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("DELETE FROM categories", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("purpose = 'product_image'", CatalogImportDatabaseSql.ResetCatalog);
        Assert.Contains("storage_key LIKE 'storage/products/catalog-import/%'", CatalogImportDatabaseSql.ResetCatalog);
        Assert.DoesNotContain("DELETE FROM request_items", CatalogImportDatabaseSql.ResetCatalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM requests", CatalogImportDatabaseSql.ResetCatalog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CountResetImpactSql_CountsAllDestructiveResetTargets()
    {
        Assert.Contains("AS \"Categories\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM categories", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"Products\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM products", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"ProductImages\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM product_images", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"StoredProductImageFiles\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM stored_files", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("purpose = 'product_image'", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("storage_key LIKE 'storage/products/catalog-import/%'", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"ProductAttributeValues\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM product_attribute_values", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"AttributeValueAliases\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM attribute_value_aliases", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"AttributeOptions\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM attribute_options", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("AS \"CategoryAttributes\"", CatalogImportDatabaseSql.CountResetImpact);
        Assert.Contains("FROM category_attributes", CatalogImportDatabaseSql.CountResetImpact);
    }

    [Fact]
    public void ApplyResult_CanCarryResetImpactOnlyWhenResetRuns()
    {
        var noResetResult = new CatalogImportApplyResult(
            CategoriesProcessed: 1,
            ProductsProcessed: 2,
            ImagesProcessed: 3);
        var impact = new CatalogImportResetImpact(
            Categories: 4,
            Products: 5,
            ProductImages: 6,
            StoredProductImageFiles: 7,
            ProductAttributeValues: 8,
            AttributeValueAliases: 9,
            AttributeOptions: 10,
            CategoryAttributes: 11);
        var resetResult = noResetResult with { ResetImpact = impact };

        Assert.Null(noResetResult.ResetImpact);
        Assert.Same(impact, resetResult.ResetImpact);
    }

    [Fact]
    public async Task ApplyAsync_RefusesResetBeforeOpeningConnection_WhenEnvironmentIsNotExplicitlyAllowed()
    {
        var database = new CatalogImportDatabase("Host=127.0.0.1;Username=not-used;Password=not-used;Database=not-used");
        var plan = new CatalogImportPlan(
            new CatalogImportSummary(0, 0, 0, 0, 0, 0),
            [],
            [],
            []);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            database.ApplyAsync(
                plan,
                new CatalogImportApplyOptions(ResetCatalog: true, AllowResetInCurrentEnvironment: false)));

        Assert.Contains("explicitly approved dev/QA", exception.Message);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            global::System.IO.Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (global::System.IO.Directory.Exists(Path))
            {
                global::System.IO.Directory.Delete(Path, recursive: true);
            }
        }
    }

    private static string ReadCatalogImportDatabaseSource()
    {
        var sourceFile = Path.Combine(
            FindRepositoryRoot(),
            "apps",
            "catalog-import.core",
            "Database",
            "CatalogImportDatabase.cs");

        return File.ReadAllText(sourceFile);
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var methodIndex = source.IndexOf(methodName, StringComparison.Ordinal);
        Assert.True(methodIndex >= 0, $"Method '{methodName}' was not found.");
        var bodyStart = source.IndexOf('{', methodIndex);
        Assert.True(bodyStart >= 0, $"Method '{methodName}' body was not found.");

        var depth = 0;
        for (var index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[bodyStart..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' body was not closed.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
