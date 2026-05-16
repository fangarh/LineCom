using System.Text.Json;
using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageManifestReaderTests
{
    [Fact]
    public void ReadAcceptedGroupsItemsByExternalIdAndKeepsMainOrder()
    {
        using var temp = new TemporaryDirectory();
        var image = Path.Combine(temp.Path, "101-a.png");
        File.WriteAllText(image, "png");
        var manifest = Path.Combine(temp.Path, "manifest.json");
        File.WriteAllText(
            manifest,
            JsonSerializer.Serialize(new
            {
                items = new object[]
                {
                    new
                    {
                        assetKey = "101-a",
                        externalId = "101",
                        status = "downloaded_png",
                        file = image,
                        checksum = new string('a', 64),
                        contentType = "image/png",
                        isMain = true,
                        visualReviewStatus = "accepted_operator_review",
                        rightsStatus = "requires-permission"
                    },
                    new
                    {
                        assetKey = "101-b",
                        externalId = "101",
                        status = "downloaded_png",
                        file = image,
                        checksum = new string('b', 64),
                        contentType = "image/png",
                        isMain = false,
                        visualReviewStatus = "accepted_operator_review",
                        rightsStatus = "requires-permission"
                    }
                }
            }),
            global::System.Text.Encoding.UTF8);

        var result = ReviewedProductImageManifestReader.ReadAcceptedByExternalId(manifest);

        Assert.True(result.ContainsKey("101"));
        Assert.Equal(2, result["101"].Count);
        Assert.True(result["101"][0].IsMain);
        Assert.False(result["101"][1].IsMain);
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
