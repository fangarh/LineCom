using LineCom.Api.Infrastructure.Hosting;
using LineCom.Api.Infrastructure.Storage;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class StorageDiagnosticsService : IStorageDiagnosticsService
{
    private const int DefaultMaxItems = 100;
    private const int MinMaxItems = 1;
    private const int MaxMaxItems = 500;

    private readonly IStorageDiagnosticsRepository repository;
    private readonly IAdminCatalogStaffGuard staffGuard;
    private readonly LocalStoredFileOptions options;
    private readonly IHostEnvironment environment;

    public StorageDiagnosticsService(
        IStorageDiagnosticsRepository repository,
        IAdminCatalogStaffGuard staffGuard,
        IOptions<LocalStoredFileOptions> options,
        IHostEnvironment environment)
    {
        this.repository = repository;
        this.staffGuard = staffGuard;
        this.options = options.Value;
        this.environment = environment;
    }

    public async Task<AdminStorageDiagnosticsResponse> GetDiagnosticsAsync(
        HttpContext httpContext,
        int? maxItems = null,
        CancellationToken cancellationToken = default)
    {
        await staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var limit = ClampMaxItems(maxItems);
        var rootPath = LocalStoragePathPolicy.ResolveRootPath(options.RootPath, environment.ContentRootPath);
        var rows = await repository.ListStoredFilesAsync(cancellationToken);
        var rowKeys = rows
            .Select(row => row.StorageKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var diskKeys = EnumerateStorageKeys(rootPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingFiles = rows
            .Where(row => IsActive(row) && !diskKeys.Contains(row.StorageKey))
            .OrderBy(row => row.StorageKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Id)
            .Select(ToStoredFileItem)
            .ToArray();

        var untrackedFiles = diskKeys
            .Where(storageKey => !rowKeys.Contains(storageKey))
            .OrderBy(storageKey => storageKey, StringComparer.OrdinalIgnoreCase)
            .Select(storageKey => new AdminStorageDiagnosticsUntrackedFileItem(storageKey))
            .ToArray();

        var staleDeletedRows = rows
            .Where(row => IsDeleted(row) && diskKeys.Contains(row.StorageKey))
            .OrderBy(row => row.StorageKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Id)
            .Select(ToStoredFileItem)
            .ToArray();

        var orphanedRows = rows
            .Where(IsOrphaned)
            .OrderBy(row => row.StorageKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Id)
            .Select(row => ToOrphanedRowItem(row, diskKeys.Contains(row.StorageKey)))
            .ToArray();

        return new AdminStorageDiagnosticsResponse(
            new AdminStorageDiagnosticsSummary(
                missingFiles.Length,
                untrackedFiles.Length,
                staleDeletedRows.Length,
                orphanedRows.Length),
            Bounded(missingFiles, limit),
            Bounded(untrackedFiles, limit),
            Bounded(staleDeletedRows, limit),
            Bounded(orphanedRows, limit));
    }

    private static IReadOnlyList<string> EnumerateStorageKeys(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Select(filePath => LocalStoragePathPolicy.ToStorageKey(rootPath, filePath))
            .OrderBy(storageKey => storageKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static AdminStorageDiagnosticsList<T> Bounded<T>(IReadOnlyList<T> items, int limit)
    {
        var boundedItems = items.Take(limit).ToArray();
        return new AdminStorageDiagnosticsList<T>(
            boundedItems,
            items.Count,
            Truncated: items.Count > boundedItems.Length);
    }

    private static int ClampMaxItems(int? maxItems)
    {
        if (maxItems is null)
        {
            return DefaultMaxItems;
        }

        return Math.Clamp(maxItems.Value, MinMaxItems, MaxMaxItems);
    }

    private static bool IsActive(StorageDiagnosticsStoredFileRecord row)
    {
        return string.Equals(row.Status, "active", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeleted(StorageDiagnosticsStoredFileRecord row)
    {
        return string.Equals(row.Status, "deleted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOrphaned(StorageDiagnosticsStoredFileRecord row)
    {
        return string.Equals(row.Status, "orphaned", StringComparison.OrdinalIgnoreCase);
    }

    private static AdminStorageDiagnosticsStoredFileItem ToStoredFileItem(
        StorageDiagnosticsStoredFileRecord row)
    {
        return new AdminStorageDiagnosticsStoredFileItem(
            row.Id,
            row.StorageKey,
            row.Purpose,
            row.Status,
            row.SizeBytes,
            row.Checksum,
            row.CreatedAt);
    }

    private static AdminStorageDiagnosticsOrphanedRowItem ToOrphanedRowItem(
        StorageDiagnosticsStoredFileRecord row,
        bool fileExists)
    {
        return new AdminStorageDiagnosticsOrphanedRowItem(
            row.Id,
            row.StorageKey,
            row.Purpose,
            row.Status,
            row.SizeBytes,
            row.Checksum,
            row.CreatedAt,
            fileExists);
    }
}
