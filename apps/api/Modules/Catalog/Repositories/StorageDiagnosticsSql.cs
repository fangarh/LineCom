namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class StorageDiagnosticsSql
{
    public const string ListStoredFiles = """
        SELECT
            id AS "Id",
            storage_key AS "StorageKey",
            purpose AS "Purpose",
            status AS "Status",
            size_bytes AS "SizeBytes",
            checksum AS "Checksum",
            created_at AS "CreatedAt"
        FROM stored_files
        ORDER BY storage_key, id;
        """;
}
