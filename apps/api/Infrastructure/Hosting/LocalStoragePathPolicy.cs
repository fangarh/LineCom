using Microsoft.Extensions.FileProviders;

namespace LineCom.Api.Infrastructure.Hosting;

internal static class LocalStoragePathPolicy
{
    public const string StorageKeyPrefix = "storage/";

    public static readonly IReadOnlyList<PublicStoragePrefix> PublicPrefixes =
    [
        new("products", "/storage/products"),
        new("brands", "/storage/brands")
    ];

    public static string ResolveRootPath(string? configuredRootPath, string contentRootPath)
    {
        var rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? Path.Combine(contentRootPath, "storage")
            : configuredRootPath;

        if (!Path.IsPathRooted(rootPath))
        {
            rootPath = Path.Combine(contentRootPath, rootPath);
        }

        return Path.GetFullPath(rootPath);
    }

    public static string ToStorageKey(string rootPath, string physicalPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, physicalPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Trim('/');

        return $"{StorageKeyPrefix}{relativePath}";
    }

    public static PhysicalFileProvider CreateFileProvider(string rootPath, string publicDirectory)
    {
        return new PhysicalFileProvider(Path.Combine(rootPath, publicDirectory));
    }
}

internal sealed record PublicStoragePrefix(string Directory, string RequestPath);
