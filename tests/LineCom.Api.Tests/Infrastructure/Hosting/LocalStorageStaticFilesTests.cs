using System.Net;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Infrastructure.Hosting;

public sealed class LocalStorageStaticFilesTests
{
    [Theory]
    [InlineData("products/catalog/cable.jpg", "/storage/products/catalog/cable.jpg")]
    [InlineData("brands/logo.png", "/storage/brands/logo.png")]
    public async Task StoragePublicImageDirectories_ReturnFiles(string relativePath, string requestPath)
    {
        using var tempDirectory = new TempDirectory();
        await WriteFileAsync(tempDirectory.Path, relativePath, "public-image");
        await WriteFileAsync(tempDirectory.Path, "import/source.json", "private-import");

        await using var factory = CreateFactory(tempDirectory.Path);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(requestPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
        Assert.Equal("public-image", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("import/source.json", "/storage/import/source.json")]
    [InlineData("export/result.csv", "/storage/export/result.csv")]
    [InlineData("temp/upload.tmp", "/storage/temp/upload.tmp")]
    public async Task StoragePrivateLikeDirectories_ReturnNotFound(string relativePath, string requestPath)
    {
        using var tempDirectory = new TempDirectory();
        await WriteFileAsync(tempDirectory.Path, relativePath, "private-file");

        await using var factory = CreateFactory(tempDirectory.Path);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(requestPath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task WriteFileAsync(string rootPath, string relativePath, string content)
    {
        var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);
    }

    private static WebApplicationFactory<Program> CreateFactory(string storageRoot)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Storage:RootPath"] = storageRoot
                    });
                });
            });
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = global::System.IO.Path.Combine(
                global::System.IO.Path.GetTempPath(),
                Guid.NewGuid().ToString("N"));
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
