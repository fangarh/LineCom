using System.Text.Json;

namespace LineCom.CatalogImport.Core.Images;

public sealed record ReviewedProductImageManifestItem(
    string AssetKey,
    string ExternalId,
    string File,
    string Checksum,
    string ContentType,
    bool IsMain,
    string RightsStatus);

public static class ReviewedProductImageManifestReader
{
    private const string AcceptedStatus = "downloaded_png";
    private const string AcceptedReviewStatus = "accepted_operator_review";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<ReviewedProductImageManifestItem>> ReadAcceptedByExternalId(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new Dictionary<string, IReadOnlyList<ReviewedProductImageManifestItem>>(StringComparer.Ordinal);
        }

        using var stream = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<ReviewedProductImageManifest>(stream, JsonOptions);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        var groups = new Dictionary<string, List<ReviewedProductImageManifestItem>>(StringComparer.Ordinal);
        foreach (var entry in manifest?.Items ?? [])
        {
            if (!IsAccepted(entry))
            {
                continue;
            }

            var item = new ReviewedProductImageManifestItem(
                entry.AssetKey!,
                entry.ExternalId!,
                ResolveImagePath(entry.File!, directory),
                entry.Checksum!,
                entry.ContentType!,
                entry.IsMain,
                string.IsNullOrWhiteSpace(entry.RightsStatus) ? "requires-permission" : entry.RightsStatus!);
            if (!groups.TryGetValue(item.ExternalId, out var list))
            {
                list = [];
                groups[item.ExternalId] = list;
            }

            list.Add(item);
        }

        return groups.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ReviewedProductImageManifestItem>)pair.Value
                .OrderByDescending(item => item.IsMain)
                .ThenBy(item => item.AssetKey, StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
    }

    private static bool IsAccepted(ReviewedProductImageManifestEntry item)
    {
        return string.Equals(item.Status, AcceptedStatus, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.VisualReviewStatus, AcceptedReviewStatus, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.AssetKey)
            && !string.IsNullOrWhiteSpace(item.ExternalId)
            && !string.IsNullOrWhiteSpace(item.File)
            && !string.IsNullOrWhiteSpace(item.Checksum)
            && string.Equals(item.ContentType, "image/png", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveImagePath(string filePath, string? manifestDirectory)
    {
        if (Path.IsPathFullyQualified(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        var directory = string.IsNullOrWhiteSpace(manifestDirectory)
            ? null
            : new DirectoryInfo(manifestDirectory);
        while (directory is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory.FullName, filePath));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return filePath;
    }

    private sealed class ReviewedProductImageManifest
    {
        public IReadOnlyList<ReviewedProductImageManifestEntry> Items { get; init; } = [];
    }

    private sealed class ReviewedProductImageManifestEntry
    {
        public string? AssetKey { get; init; }
        public string? ExternalId { get; init; }
        public string? Status { get; init; }
        public string? File { get; init; }
        public string? Checksum { get; init; }
        public string? ContentType { get; init; }
        public bool IsMain { get; init; }
        public string? VisualReviewStatus { get; init; }
        public string? RightsStatus { get; init; }
    }
}
