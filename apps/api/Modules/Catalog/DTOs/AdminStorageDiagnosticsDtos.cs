namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminStorageDiagnosticsResponse(
    AdminStorageDiagnosticsSummary Summary,
    AdminStorageDiagnosticsList<AdminStorageDiagnosticsStoredFileItem> MissingFiles,
    AdminStorageDiagnosticsList<AdminStorageDiagnosticsUntrackedFileItem> UntrackedFiles,
    AdminStorageDiagnosticsList<AdminStorageDiagnosticsStoredFileItem> StaleDeletedRows,
    AdminStorageDiagnosticsList<AdminStorageDiagnosticsOrphanedRowItem> OrphanedRows);

public sealed record AdminStorageDiagnosticsSummary(
    int MissingFiles,
    int UntrackedFiles,
    int StaleDeletedRows,
    int OrphanedRows);

public sealed record AdminStorageDiagnosticsList<T>(
    IReadOnlyList<T> Items,
    int Count,
    bool Truncated);

public sealed record AdminStorageDiagnosticsStoredFileItem(
    Guid Id,
    string StorageKey,
    string Purpose,
    string Status,
    long SizeBytes,
    string Checksum,
    DateTimeOffset CreatedAt);

public sealed record AdminStorageDiagnosticsOrphanedRowItem(
    Guid Id,
    string StorageKey,
    string Purpose,
    string Status,
    long SizeBytes,
    string Checksum,
    DateTimeOffset CreatedAt,
    bool FileExists);

public sealed record AdminStorageDiagnosticsUntrackedFileItem(string StorageKey);
