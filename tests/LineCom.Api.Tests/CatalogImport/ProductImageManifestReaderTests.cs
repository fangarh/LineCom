using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ProductImageManifestReaderTests
{
    [Fact]
    public void ReadAcceptedBySourceRow_ReturnsEmptyDictionary_WhenPathIsMissing()
    {
        var imagesForNullPath = ProductImageManifestReader.ReadAcceptedBySourceRow(null);
        var imagesForEmptyPath = ProductImageManifestReader.ReadAcceptedBySourceRow("");
        var imagesForMissingPath = ProductImageManifestReader.ReadAcceptedBySourceRow(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json"));

        Assert.Empty(imagesForNullPath);
        Assert.Empty(imagesForEmptyPath);
        Assert.Empty(imagesForMissingPath);
    }

    [Fact]
    public void ReadAcceptedBySourceRow_ReturnsOnlyAcceptedDownloadedPngImages()
    {
        using var temp = new TemporaryDirectory();
        var manifest = Path.Combine(temp.Path, "manifest.json");
        File.WriteAllText(
            manifest,
            """
            {
              "items": [
                {
                  "assetKey": "accepted",
                  "status": "downloaded_png",
                  "file": "Assets/product-images/accepted.png",
                  "sourceRows": [10, 11],
                  "visualReviewStatus": "accepted_visual_scan",
                  "rightsStatus": "requires-permission"
                },
                {
                  "assetKey": "failed",
                  "status": "failed",
                  "sourceRows": [12],
                  "visualReviewStatus": "accepted_visual_scan"
                },
                {
                  "assetKey": "no-rights",
                  "status": "downloaded_png",
                  "file": "Assets/product-images/no-rights.png",
                  "sourceRows": [13],
                  "visualReviewStatus": "accepted_visual_scan"
                },
                {
                  "assetKey": "no-source-rows",
                  "status": "downloaded_png",
                  "file": "Assets/product-images/no-source-rows.png",
                  "visualReviewStatus": "accepted_visual_scan"
                },
                {
                  "assetKey": "trusted-tktdf",
                  "status": "downloaded_png",
                  "file": "Assets/product-images/tktdf/trusted-tktdf.png",
                  "sourceRows": [14],
                  "visualReviewStatus": "trusted_source_tktdf",
                  "rightsStatus": "requires-permission"
                }
              ]
            }
            """);

        var images = ProductImageManifestReader.ReadAcceptedBySourceRow(manifest);

        Assert.True(images.ContainsKey(10));
        Assert.True(images.ContainsKey(11));
        Assert.False(images.ContainsKey(12));
        Assert.False(images.ContainsKey(0));
        Assert.Equal("accepted", images[10].AssetKey);
        Assert.Equal("Assets/product-images/accepted.png", images[10].File);
        Assert.Equal("requires-permission", images[10].RightsStatus);
        Assert.Equal("no-rights", images[13].AssetKey);
        Assert.Equal("Assets/product-images/no-rights.png", images[13].File);
        Assert.Equal("requires-permission", images[13].RightsStatus);
        Assert.Equal("trusted-tktdf", images[14].AssetKey);
        Assert.Equal("Assets/product-images/tktdf/trusted-tktdf.png", images[14].File);
    }

    [Fact]
    public void ReadAcceptedBySourceRow_ResolvesRepositoryRelativeImagePathsFromManifestLocation()
    {
        using var temp = new TemporaryDirectory();
        var manifestDirectory = Path.Combine(temp.Path, "Assets", "product-images");
        var imageDirectory = Path.Combine(manifestDirectory, "tktdf");
        Directory.CreateDirectory(imageDirectory);
        var imagePath = Path.Combine(imageDirectory, "image.png");
        File.WriteAllBytes(imagePath, [1, 2, 3]);
        var manifest = Path.Combine(manifestDirectory, "tktdf_manifest.json");
        File.WriteAllText(
            manifest,
            """
            {
              "items": [
                {
                  "assetKey": "image",
                  "status": "downloaded_png",
                  "file": "Assets/product-images/tktdf/image.png",
                  "sourceRows": [117],
                  "visualReviewStatus": "trusted_source_tktdf",
                  "rightsStatus": "requires-permission"
                }
              ]
            }
            """);

        var originalCurrentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Path.GetTempPath();

            var images = ProductImageManifestReader.ReadAcceptedBySourceRow(manifest);

            Assert.Equal(Path.GetFullPath(imagePath), images[117].File);
        }
        finally
        {
            Environment.CurrentDirectory = originalCurrentDirectory;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
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
