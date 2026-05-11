namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductImagesResponse(IReadOnlyList<AdminProductImageDto> Items);

public sealed record AdminProductImageDto(
    Guid Id,
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Alt,
    string? Title,
    int SortOrder,
    bool IsMain,
    DateTimeOffset CreatedAt);

public sealed record UpdateAdminProductImageCommand(string? Alt, string? Title);

public sealed record UpdateAdminProductImageOrderCommand(IReadOnlyList<Guid> ImageIds);

public sealed record AdminBrandLogoDto(
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum);
