using System.Text.Json;

namespace LineCom.CatalogImport.Core.Images;

public static class ProductImageManifestReader
{
    public const string DefaultRightsStatus = "requires-permission";

    private const string AcceptedStatus = "downloaded_png";
    private static readonly string[] AcceptedVisualReviewStatuses =
    [
        "accepted_visual_scan",
        "trusted_source_tktdf"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyDictionary<int, ProductImageManifestItem> ReadAcceptedBySourceRow(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new Dictionary<int, ProductImageManifestItem>();
        }

        using var stream = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<ProductImageManifest>(stream, JsonOptions);
        if (manifest?.Items is null || manifest.Items.Count == 0)
        {
            return new Dictionary<int, ProductImageManifestItem>();
        }

        var result = new Dictionary<int, ProductImageManifestItem>();
        foreach (var item in manifest.Items)
        {
            if (!IsAccepted(item))
            {
                continue;
            }

            var image = new ProductImageManifestItem(
                item.AssetKey,
                item.File,
                NormalizeRightsStatus(item.RightsStatus));
            foreach (var sourceRow in item.SourceRows ?? [])
            {
                result.TryAdd(sourceRow, image);
            }
        }

        return result;
    }

    private static bool IsAccepted(ProductImageManifestEntry item)
    {
        return !string.IsNullOrWhiteSpace(item.AssetKey)
            && !string.IsNullOrWhiteSpace(item.File)
            && item.SourceRows is { Count: > 0 }
            && string.Equals(item.Status, AcceptedStatus, StringComparison.OrdinalIgnoreCase)
            && AcceptedVisualReviewStatuses.Any(
                status => string.Equals(item.VisualReviewStatus, status, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRightsStatus(string? rightsStatus)
    {
        return string.IsNullOrWhiteSpace(rightsStatus) ? DefaultRightsStatus : rightsStatus;
    }
}
