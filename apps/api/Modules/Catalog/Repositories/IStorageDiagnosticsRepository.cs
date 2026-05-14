namespace LineCom.Api.Modules.Catalog.Repositories;

public interface IStorageDiagnosticsRepository
{
    Task<IReadOnlyList<StorageDiagnosticsStoredFileRecord>> ListStoredFilesAsync(
        CancellationToken cancellationToken = default);
}

public sealed record StorageDiagnosticsStoredFileRecord(
    Guid Id,
    string StorageKey,
    string Purpose,
    string Status,
    long SizeBytes,
    string Checksum,
    DateTimeOffset CreatedAt);
