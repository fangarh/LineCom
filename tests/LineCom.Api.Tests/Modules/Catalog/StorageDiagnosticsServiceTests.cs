using LineCom.Api.Infrastructure.Storage;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class StorageDiagnosticsServiceTests
{
    private static readonly Guid ActiveMissingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DeletedExistingId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrphanedExistingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OrphanedMissingId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetDiagnosticsAsync_ClassifiesDbDiskDrift()
    {
        using var tempDirectory = new TempDirectory();
        await WriteFileAsync(tempDirectory.Path, "products/deleted.jpg", "deleted");
        await WriteFileAsync(tempDirectory.Path, "products/orphaned.jpg", "orphaned");
        await WriteFileAsync(tempDirectory.Path, "products/untracked.jpg", "untracked");

        var service = CreateService(
            tempDirectory.Path,
            new[]
            {
                StoredFile(ActiveMissingId, "storage/products/missing.jpg", "product_image", "active"),
                StoredFile(DeletedExistingId, "storage/products/deleted.jpg", "product_image", "deleted"),
                StoredFile(OrphanedExistingId, "storage/products/orphaned.jpg", "product_image", "orphaned"),
                StoredFile(OrphanedMissingId, "storage/products/orphaned-missing.jpg", "product_image", "orphaned")
            });

        var response = await service.GetDiagnosticsAsync(new DefaultHttpContext(), maxItems: 100);

        Assert.Equal(1, response.Summary.MissingFiles);
        Assert.Equal(1, response.Summary.UntrackedFiles);
        Assert.Equal(1, response.Summary.StaleDeletedRows);
        Assert.Equal(2, response.Summary.OrphanedRows);

        Assert.Equal("storage/products/missing.jpg", Assert.Single(response.MissingFiles.Items).StorageKey);
        Assert.Equal("storage/products/untracked.jpg", Assert.Single(response.UntrackedFiles.Items).StorageKey);
        Assert.Equal("storage/products/deleted.jpg", Assert.Single(response.StaleDeletedRows.Items).StorageKey);

        var orphanedRows = response.OrphanedRows.Items.OrderBy(item => item.StorageKey).ToArray();
        Assert.Equal("storage/products/orphaned-missing.jpg", orphanedRows[0].StorageKey);
        Assert.False(orphanedRows[0].FileExists);
        Assert.Equal("storage/products/orphaned.jpg", orphanedRows[1].StorageKey);
        Assert.True(orphanedRows[1].FileExists);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_UsesFullCountsAndBoundedDetails()
    {
        using var tempDirectory = new TempDirectory();
        var rows = Enumerable.Range(0, 3)
            .Select(index => StoredFile(
                Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-{index + 1:000000000000}"),
                $"storage/products/missing-{index}.jpg",
                "product_image",
                "active"))
            .ToArray();
        var service = CreateService(tempDirectory.Path, rows);

        var response = await service.GetDiagnosticsAsync(new DefaultHttpContext(), maxItems: 2);

        Assert.Equal(3, response.Summary.MissingFiles);
        Assert.Equal(3, response.MissingFiles.Count);
        Assert.Equal(2, response.MissingFiles.Items.Count);
        Assert.True(response.MissingFiles.Truncated);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_DoesNotExposeAbsoluteStorageRoot()
    {
        using var tempDirectory = new TempDirectory();
        await WriteFileAsync(tempDirectory.Path, "products/untracked.jpg", "untracked");
        var service = CreateService(tempDirectory.Path, Array.Empty<StorageDiagnosticsStoredFileRecord>());

        var response = await service.GetDiagnosticsAsync(new DefaultHttpContext(), maxItems: 100);

        var untracked = Assert.Single(response.UntrackedFiles.Items);
        Assert.Equal("storage/products/untracked.jpg", untracked.StorageKey);
        Assert.DoesNotContain(tempDirectory.Path, untracked.StorageKey, StringComparison.OrdinalIgnoreCase);
    }

    private static StorageDiagnosticsService CreateService(
        string rootPath,
        IReadOnlyList<StorageDiagnosticsStoredFileRecord> rows)
    {
        return new StorageDiagnosticsService(
            new FakeStorageDiagnosticsRepository(rows),
            new AllowingStaffGuard(),
            Options.Create(new LocalStoredFileOptions { RootPath = rootPath }),
            new FakeHostEnvironment(rootPath));
    }

    private static StorageDiagnosticsStoredFileRecord StoredFile(
        Guid id,
        string storageKey,
        string purpose,
        string status)
    {
        return new StorageDiagnosticsStoredFileRecord(
            id,
            storageKey,
            purpose,
            status,
            SizeBytes: 10,
            Checksum: "checksum",
            CreatedAt: DateTimeOffset.Parse("2026-05-14T00:00:00Z"));
    }

    private static async Task WriteFileAsync(string rootPath, string relativePath, string content)
    {
        var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);
    }

    private sealed class FakeStorageDiagnosticsRepository : IStorageDiagnosticsRepository
    {
        private readonly IReadOnlyList<StorageDiagnosticsStoredFileRecord> rows;

        public FakeStorageDiagnosticsRepository(IReadOnlyList<StorageDiagnosticsStoredFileRecord> rows)
        {
            this.rows = rows;
        }

        public Task<IReadOnlyList<StorageDiagnosticsStoredFileRecord>> ListStoredFilesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(rows);
        }
    }

    private sealed class AllowingStaffGuard : IAdminCatalogStaffGuard
    {
        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CurrentUserDto(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "Seller",
                "seller@example.com",
                "+79000000000",
                "seller"));
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "LineCom.Api.Tests";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = global::System.IO.Path.Combine(
                global::System.IO.Path.GetTempPath(),
                Guid.NewGuid().ToString("N"));
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
