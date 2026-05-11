using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Infrastructure.Storage;

public sealed class InvalidLocalStoredFileException : Exception
{
    public InvalidLocalStoredFileException(string message)
        : base(message)
    {
    }
}

public sealed class LocalStoredFileWriter : ILocalStoredFileWriter
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const string StorageKeyPrefix = "storage/";

    private static readonly IReadOnlyDictionary<string, string> SupportedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly string rootPath;

    public LocalStoredFileWriter(IOptions<LocalStoredFileOptions> options)
    {
        var configuredRootPath = options.Value.RootPath;
        rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "storage")
            : configuredRootPath;
    }

    public async Task<LocalStoredFileDraft> SaveAsync(
        IFormFile file,
        Guid fileId,
        string purpose,
        string storageDirectory,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
        {
            throw new InvalidLocalStoredFileException("Invalid image size.");
        }

        if (!SupportedContentTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new InvalidLocalStoredFileException("Invalid image content type.");
        }

        var normalizedDirectory = NormalizeStorageDirectory(storageDirectory);
        var storageKey = $"{StorageKeyPrefix}{normalizedDirectory}/{fileId:N}{extension}";
        var physicalPath = ResolvePhysicalPath(storageKey);
        var directoryPath = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var inputStream = file.OpenReadStream();
        await using var outputStream = new FileStream(
            physicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var read = await inputStream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (totalBytes > MaxImageSizeBytes)
            {
                outputStream.Close();
                File.Delete(physicalPath);
                throw new InvalidLocalStoredFileException("Invalid image size.");
            }

            hash.AppendData(buffer.AsSpan(0, read));
            await outputStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return new LocalStoredFileDraft(
            fileId,
            storageKey,
            file.FileName,
            file.ContentType,
            totalBytes,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            purpose,
            createdByUserId);
    }

    public Task DeletePhysicalFileIfExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var physicalPath = ResolvePhysicalPath(storageKey);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePhysicalPath(string storageKey)
    {
        if (!storageKey.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLocalStoredFileException("Invalid storage key.");
        }

        var relativePath = storageKey[StorageKeyPrefix.Length..].Replace('\\', '/');
        var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var rootFullPath = Path.GetFullPath(rootPath);
        var physicalPath = Path.GetFullPath(Path.Combine([rootFullPath, .. pathParts]));
        var rootWithSeparator = rootFullPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootFullPath
            : rootFullPath + Path.DirectorySeparatorChar;

        if (!physicalPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLocalStoredFileException("Invalid storage key.");
        }

        return physicalPath;
    }

    private static string NormalizeStorageDirectory(string storageDirectory)
    {
        var normalized = storageDirectory.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase)
            || normalized.Split('/').Any(part => part == ".."))
        {
            throw new InvalidLocalStoredFileException("Invalid storage directory.");
        }

        return normalized;
    }
}
