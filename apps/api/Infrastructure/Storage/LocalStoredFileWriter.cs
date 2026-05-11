using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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

    public LocalStoredFileWriter(IOptions<LocalStoredFileOptions> options, IHostEnvironment environment)
    {
        var configuredRootPath = options.Value.RootPath;
        rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? Path.Combine(environment.ContentRootPath, "storage")
            : configuredRootPath;

        if (!Path.IsPathRooted(rootPath))
        {
            rootPath = Path.Combine(environment.ContentRootPath, rootPath);
        }
    }

    public async Task<LocalStoredFileDraft> SaveAsync(
        IFormFile file,
        Guid fileId,
        string purpose,
        string storageDirectory,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            throw new InvalidLocalStoredFileException("Invalid image file.");
        }

        if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
        {
            throw new InvalidLocalStoredFileException("Invalid image size.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType)
            || !SupportedContentTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new InvalidLocalStoredFileException("Invalid image content type.");
        }

        var originalFileName = NormalizeOriginalFileName(file.FileName);
        var normalizedDirectory = NormalizeStorageDirectory(storageDirectory);
        var storageKey = $"{StorageKeyPrefix}{normalizedDirectory}/{fileId:N}{extension}";
        var physicalPath = ResolvePhysicalPath(storageKey);
        var directoryPath = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var temporaryPath = Path.Combine(
            directoryPath ?? rootPath,
            $".{fileId:N}.{Guid.NewGuid():N}.tmp");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long totalBytes = 0;

        try
        {
            await using var inputStream = file.OpenReadStream();
            await using var outputStream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

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
                    throw new InvalidLocalStoredFileException("Invalid image size.");
                }

                hash.AppendData(buffer.AsSpan(0, read));
                await outputStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await outputStream.FlushAsync(cancellationToken);
        }
        catch
        {
            DeleteFileIfExists(temporaryPath);
            throw;
        }

        try
        {
            File.Move(temporaryPath, physicalPath, overwrite: true);
        }
        catch
        {
            DeleteFileIfExists(temporaryPath);
            throw;
        }

        if (totalBytes <= 0)
        {
            DeleteFileIfExists(physicalPath);
            throw new InvalidLocalStoredFileException("Invalid image size.");
        }

        try
        {
            return new LocalStoredFileDraft(
                fileId,
                storageKey,
                originalFileName,
                file.ContentType,
                totalBytes,
                Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
                purpose,
                createdByUserId);
        }
        catch
        {
            DeleteFileIfExists(physicalPath);
            throw;
        }
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
        if (string.IsNullOrWhiteSpace(storageKey)
            || !storageKey.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLocalStoredFileException("Invalid storage key.");
        }

        var relativePath = storageKey[StorageKeyPrefix.Length..].Replace('\\', '/');
        var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0 || pathParts.Any(part => part == ".."))
        {
            throw new InvalidLocalStoredFileException("Invalid storage key.");
        }

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
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new InvalidLocalStoredFileException("Invalid storage directory.");
        }

        var normalized = storageDirectory.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase)
            || normalized.Split('/').Any(part => part == ".."))
        {
            throw new InvalidLocalStoredFileException("Invalid storage directory.");
        }

        return normalized;
    }

    private static string NormalizeOriginalFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName.Replace('\\', Path.DirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(normalized) || normalized is "." or "..")
        {
            throw new InvalidLocalStoredFileException("Invalid original file name.");
        }

        return normalized;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
