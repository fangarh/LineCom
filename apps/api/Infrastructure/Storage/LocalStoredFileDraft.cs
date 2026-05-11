namespace LineCom.Api.Infrastructure.Storage;

public sealed record LocalStoredFileDraft(
    Guid Id,
    string StorageKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Purpose,
    Guid CreatedByUserId);
