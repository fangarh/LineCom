using System.Security.Cryptography;
using LineCom.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Tests.Infrastructure.Storage;

public sealed class LocalStoredFileWriterTests
{
    [Fact]
    public async Task SaveAsync_WritesFileUnderStorageRootAndReturnsStoredFileDraft()
    {
        using var tempDirectory = new TempDirectory();
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var bytes = "image-bytes"u8.ToArray();
        var file = CreateFormFile("cable.JPG", "image/jpeg", bytes);
        var writer = CreateWriter(tempDirectory.Path);

        var draft = await writer.SaveAsync(
            file,
            fileId,
            "product_image",
            "products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            userId);

        Assert.Equal(fileId, draft.Id);
        Assert.Equal(
            "storage/products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg",
            draft.StorageKey);
        Assert.Equal("cable.JPG", draft.OriginalFileName);
        Assert.Equal("image/jpeg", draft.ContentType);
        Assert.Equal(bytes.Length, draft.SizeBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), draft.Checksum);
        Assert.Equal("product_image", draft.Purpose);
        Assert.Equal(userId, draft.CreatedByUserId);

        var physicalPath = Path.Combine(
            tempDirectory.Path,
            "products",
            "admin",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg");
        Assert.True(File.Exists(physicalPath));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(physicalPath));
    }

    [Fact]
    public async Task SaveAsync_RejectsUnsupportedContentType()
    {
        using var tempDirectory = new TempDirectory();
        var file = CreateFormFile("notes.txt", "text/plain", "not-image"u8.ToArray());
        var writer = CreateWriter(tempDirectory.Path);

        var exception = await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(
                file,
                Guid.NewGuid(),
                "product_image",
                "products/admin",
                Guid.NewGuid()));

        Assert.Equal("Invalid image content type.", exception.Message);
    }

    [Fact]
    public async Task DeletePhysicalFileIfExistsAsync_RemovesOnlyPathInsideStorageRoot()
    {
        using var tempDirectory = new TempDirectory();
        var directoryPath = Path.Combine(tempDirectory.Path, "products", "admin");
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, "image.jpg");
        await File.WriteAllBytesAsync(filePath, "image-bytes"u8.ToArray());
        var writer = CreateWriter(tempDirectory.Path);

        await writer.DeletePhysicalFileIfExistsAsync("storage/products/admin/image.jpg");

        Assert.False(File.Exists(filePath));
    }

    private static LocalStoredFileWriter CreateWriter(string rootPath)
    {
        return new LocalStoredFileWriter(Options.Create(new LocalStoredFileOptions
        {
            RootPath = rootPath
        }));
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
