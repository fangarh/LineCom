using System.Text.Json.Serialization;

namespace LineCom.CatalogImport.Core.Images;

public sealed record ProductImageManifestItem(
    [property: JsonPropertyName("assetKey")] string AssetKey,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("rightsStatus")] string RightsStatus);

internal sealed record ProductImageManifest(
    [property: JsonPropertyName("items")] IReadOnlyList<ProductImageManifestEntry>? Items);

internal sealed record ProductImageManifestEntry(
    [property: JsonPropertyName("assetKey")] string AssetKey,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("sourceRows")] IReadOnlyList<int>? SourceRows,
    [property: JsonPropertyName("visualReviewStatus")] string? VisualReviewStatus,
    [property: JsonPropertyName("rightsStatus")] string? RightsStatus);
