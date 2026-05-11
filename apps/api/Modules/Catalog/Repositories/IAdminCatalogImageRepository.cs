using LineCom.Api.Infrastructure.Storage;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminProductImageRecord(
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

public sealed record AdminProductImageMetadataUpdate(string Alt, string? Title);

internal sealed class AdminProductImageNotFoundException : Exception;

internal sealed class AdminProductImageOrderMismatchException : Exception;

public interface IAdminCatalogImageRepository
{
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<string?> GetProductNameAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> GetProductImagesAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync(
        Guid productId,
        IReadOnlyList<LocalStoredFileDraft> files,
        string defaultAlt,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageRecord?> UpdateProductImageAsync(
        Guid productId,
        Guid imageId,
        AdminProductImageMetadataUpdate command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> UpdateProductImageOrderAsync(
        Guid productId,
        IReadOnlyList<Guid> imageIds,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageRecord?> SetMainProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);
}
