using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Infrastructure.Storage;

public interface ILocalStoredFileWriter
{
    Task<LocalStoredFileDraft> SaveAsync(
        IFormFile file,
        Guid fileId,
        string purpose,
        string storageDirectory,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task DeletePhysicalFileIfExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
