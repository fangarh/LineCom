using LineCom.CatalogImport.Core.Database;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportStorageLifecycleTests
{
    [Fact]
    public void FormatStagingKey_CreatesPrivatePerRunRelativePath()
    {
        var stagingKey = CatalogImportDatabaseStorage.FormatStagingKey(
            "run-123",
            "storage/products/catalog-import/asset-abcdef123456.png");

        Assert.Equal(".staging/catalog-import/run-123/asset-abcdef123456.png", stagingKey);
        Assert.False(stagingKey.StartsWith("storage/", StringComparison.Ordinal));
    }

    [Fact]
    public void StageProductImage_WritesOnlyUnderConfiguredStorageRoot()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "source.png");
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var stagingKey = CatalogImportDatabaseStorage.FormatStagingKey(
            "run-1",
            "storage/products/catalog-import/asset-abcdef123456.png");
        File.WriteAllText(sourcePath, "image-bytes");

        CatalogImportDatabaseStorage.StageProductImage(sourcePath, stagingKey, storageRootPath);

        var stagedPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(storageRootPath, stagingKey);
        Assert.StartsWith(Path.GetFullPath(storageRootPath), stagedPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image-bytes", File.ReadAllText(stagedPath));
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("storage/products/../outside.png")]
    [InlineData("/storage/products/catalog-import/a.png")]
    public void ResolvePhysicalPath_RejectsTraversalAndRootedKeys(string relativeKey)
    {
        using var temp = new TemporaryDirectory();

        Assert.Throws<InvalidOperationException>(() =>
            CatalogImportDatabaseStorage.ResolvePhysicalPath(temp.Path, relativeKey));
    }

    [Fact]
    public void PromoteStagedProductImage_DoesNotOverwriteDifferentExistingPublicFile()
    {
        using var temp = new TemporaryDirectory();
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var storageKey = "storage/products/catalog-import/asset-abcdef123456.png";
        var stagingKey = CatalogImportDatabaseStorage.FormatStagingKey("run-1", storageKey);
        var stagedPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(storageRootPath, stagingKey);
        var destinationPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(storageRootPath, storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(stagedPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllText(stagedPath, "new-bytes");
        File.WriteAllText(destinationPath, "existing-bytes");

        var failure = CatalogImportDatabaseStorage.PromoteStagedProductImage(
            stagingKey,
            storageKey,
            storageRootPath,
            expectedChecksum: "not-needed-for-mismatch",
            expectedSizeBytes: 9);

        Assert.NotNull(failure);
        Assert.Equal(storageKey, failure.Key);
        Assert.Equal("existing-bytes", File.ReadAllText(destinationPath));
        Assert.True(File.Exists(stagedPath));
    }

    [Fact]
    public void PromoteStagedProductImage_ReturnsRelativeFailureWhenStagingFileIsMissing()
    {
        using var temp = new TemporaryDirectory();
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var storageKey = "storage/products/catalog-import/asset-abcdef123456.png";
        var stagingKey = CatalogImportDatabaseStorage.FormatStagingKey("run-1", storageKey);

        var failure = CatalogImportDatabaseStorage.PromoteStagedProductImage(
            stagingKey,
            storageKey,
            storageRootPath,
            expectedChecksum: "checksum",
            expectedSizeBytes: 1);

        Assert.NotNull(failure);
        Assert.Equal(storageKey, failure.Key);
        Assert.DoesNotContain(temp.Path, failure.Error);
    }

    [Fact]
    public void FindOldStagingLeftovers_ReportsPreviousRunsWithoutDeletingThem()
    {
        using var temp = new TemporaryDirectory();
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var oldRunPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(
            storageRootPath,
            ".staging/catalog-import/old-run");
        var currentRunPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(
            storageRootPath,
            ".staging/catalog-import/current-run");
        Directory.CreateDirectory(oldRunPath);
        Directory.CreateDirectory(currentRunPath);

        var leftovers = CatalogImportDatabaseStorage.FindOldStagingLeftovers(storageRootPath, "current-run");

        Assert.Contains(".staging/catalog-import/old-run", leftovers);
        Assert.DoesNotContain(".staging/catalog-import/current-run", leftovers);
        Assert.True(Directory.Exists(oldRunPath));
        Assert.True(Directory.Exists(currentRunPath));
    }

    [Fact]
    public void FindUntrackedProductImageFiles_ReportsButDoesNotDeleteUnknownFiles()
    {
        using var temp = new TemporaryDirectory();
        var storageRootPath = Path.Combine(temp.Path, "storage");
        var trackedKey = "storage/products/catalog-import/tracked.png";
        var untrackedKey = "storage/products/catalog-import/untracked.png";
        var trackedPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(storageRootPath, trackedKey);
        var untrackedPath = CatalogImportDatabaseStorage.ResolvePhysicalPath(storageRootPath, untrackedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(trackedPath)!);
        File.WriteAllText(trackedPath, "tracked");
        File.WriteAllText(untrackedPath, "untracked");

        var leftovers = CatalogImportDatabaseStorage.FindUntrackedProductImageFiles(
            storageRootPath,
            new HashSet<string>([trackedKey], StringComparer.Ordinal));

        Assert.Equal([untrackedKey], leftovers);
        Assert.True(File.Exists(trackedPath));
        Assert.True(File.Exists(untrackedPath));
    }

    [Fact]
    public void StorageConflictException_ReportsMetadataWithoutAbsolutePaths()
    {
        using var temp = new TemporaryDirectory();
        var conflict = new CatalogImportStorageConflict(
            StorageKey: "storage/products/catalog-import/asset-abcdef123456.png",
            SourceAssetKey: "asset-1",
            OriginalFileName: "image.png",
            ExpectedChecksum: "expected",
            ExpectedSizeBytes: 10,
            ExpectedContentType: "image/png",
            ExistingChecksum: "existing",
            ExistingSizeBytes: 11,
            ExistingContentType: "image/png");

        var exception = new CatalogImportStorageConflictException(conflict);

        Assert.Contains(conflict.StorageKey, exception.Message);
        Assert.Contains(conflict.SourceAssetKey, exception.Message);
        Assert.Contains(conflict.OriginalFileName, exception.Message);
        Assert.Contains(conflict.ExpectedChecksum, exception.Message);
        Assert.Contains(conflict.ExistingChecksum, exception.Message);
        Assert.DoesNotContain(temp.Path, exception.Message);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
