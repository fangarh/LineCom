using System.Security.Cryptography;
using LineCom.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
    public async Task SaveAsync_DeletesPartialFileWhenReadFails()
    {
        using var tempDirectory = new TempDirectory();
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var file = new ThrowingFormFile("cable.jpg", "image/jpeg", "partial-bytes"u8.ToArray());
        var writer = CreateWriter(tempDirectory.Path);

        await Assert.ThrowsAsync<IOException>(async () =>
            await writer.SaveAsync(
                file,
                fileId,
                "product_image",
                "products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Guid.NewGuid()));

        var productDirectory = Path.Combine(
            tempDirectory.Path,
            "products",
            "admin",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        Assert.False(File.Exists(Path.Combine(productDirectory, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")));
        Assert.Empty(Directory.EnumerateFiles(productDirectory));
    }

    [Fact]
    public async Task SaveAsync_UsesContentRootStorageWhenRootPathMissing()
    {
        using var contentRootDirectory = new TempDirectory();
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var file = CreateFormFile("cable.jpg", "image/jpeg", "image-bytes"u8.ToArray());
        var writer = new LocalStoredFileWriter(
            Options.Create(new LocalStoredFileOptions()),
            new FakeHostEnvironment(contentRootDirectory.Path));

        await writer.SaveAsync(
            file,
            fileId,
            "product_image",
            "products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Guid.NewGuid());

        Assert.True(File.Exists(Path.Combine(
            contentRootDirectory.Path,
            "storage",
            "products",
            "admin",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")));
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidInputsWithInvalidLocalStoredFileException()
    {
        using var tempDirectory = new TempDirectory();
        var writer = CreateWriter(tempDirectory.Path);
        var file = CreateFormFile("cable.jpg", "image/jpeg", "image-bytes"u8.ToArray());

        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(null!, Guid.NewGuid(), "product_image", "products/admin", Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(CreateFormFile("cable.jpg", null!, "image-bytes"u8.ToArray()), Guid.NewGuid(), "product_image", "products/admin", Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(CreateFormFile("cable.jpg", "   ", "image-bytes"u8.ToArray()), Guid.NewGuid(), "product_image", "products/admin", Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(file, Guid.NewGuid(), "product_image", null!, Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(file, Guid.NewGuid(), "product_image", "   ", Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(CreateFormFile("..", "image/jpeg", "image-bytes"u8.ToArray()), Guid.NewGuid(), "product_image", "products/admin", Guid.NewGuid()));
    }

    [Fact]
    public async Task SaveAsync_RejectsNullOriginalFileNameWithInvalidLocalStoredFileException()
    {
        using var tempDirectory = new TempDirectory();
        var writer = CreateWriter(tempDirectory.Path);
        var file = new TestFormFile(null, "image/jpeg", "image-bytes"u8.ToArray());

        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.SaveAsync(
                file,
                Guid.NewGuid(),
                "product_image",
                "products/admin",
                Guid.NewGuid()));
    }

    [Fact]
    public async Task SaveAsync_NormalizesOriginalFileName()
    {
        using var tempDirectory = new TempDirectory();
        var writer = CreateWriter(tempDirectory.Path);
        var file = CreateFormFile("..\\unsafe\\cable.JPG", "image/jpeg", "image-bytes"u8.ToArray());

        var draft = await writer.SaveAsync(
            file,
            Guid.NewGuid(),
            "product_image",
            "products/admin",
            Guid.NewGuid());

        Assert.Equal("cable.JPG", draft.OriginalFileName);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad/products/image.jpg")]
    [InlineData("storage/../outside.jpg")]
    [InlineData("storage\\..\\outside.jpg")]
    public async Task DeletePhysicalFileIfExistsAsync_RejectsInvalidStorageKeys(string? storageKey)
    {
        using var tempDirectory = new TempDirectory();
        var writer = CreateWriter(tempDirectory.Path);

        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(async () =>
            await writer.DeletePhysicalFileIfExistsAsync(storageKey!));
    }

    private static LocalStoredFileWriter CreateWriter(string rootPath)
    {
        return new LocalStoredFileWriter(
            Options.Create(new LocalStoredFileOptions
            {
                RootPath = rootPath
            }),
            new FakeHostEnvironment(rootPath));
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

    private sealed class TestFormFile : IFormFile
    {
        private readonly byte[] bytes;

        public TestFormFile(string? fileName, string contentType, byte[] bytes)
        {
            FileName = fileName!;
            ContentType = contentType;
            this.bytes = bytes;
        }

        public string ContentType { get; }

        public string ContentDisposition => string.Empty;

        public IHeaderDictionary Headers { get; } = new HeaderDictionary();

        public long Length => bytes.Length;

        public string Name => "file";

        public string FileName { get; }

        public void CopyTo(Stream target)
        {
            using var stream = OpenReadStream();
            stream.CopyTo(target);
        }

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await using var stream = OpenReadStream();
            await stream.CopyToAsync(target, cancellationToken);
        }

        public Stream OpenReadStream()
        {
            return new MemoryStream(bytes);
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "LineCom.Api.Tests";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class ThrowingFormFile : IFormFile
    {
        private readonly byte[] bytes;

        public ThrowingFormFile(string fileName, string contentType, byte[] bytes)
        {
            FileName = fileName;
            ContentType = contentType;
            this.bytes = bytes;
        }

        public string ContentType { get; }

        public string ContentDisposition => string.Empty;

        public IHeaderDictionary Headers { get; } = new HeaderDictionary();

        public long Length => bytes.Length;

        public string Name => "file";

        public string FileName { get; }

        public void CopyTo(Stream target)
        {
            throw new NotSupportedException();
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Stream OpenReadStream()
        {
            return new ThrowingReadStream(bytes);
        }
    }

    private sealed class ThrowingReadStream : MemoryStream
    {
        private bool hasRead;

        public ThrowingReadStream(byte[] buffer)
            : base(buffer)
        {
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (hasRead)
            {
                throw new IOException("Simulated read failure.");
            }

            hasRead = true;
            return await base.ReadAsync(buffer, cancellationToken);
        }
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
