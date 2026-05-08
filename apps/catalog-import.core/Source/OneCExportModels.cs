using System.Text.Json.Serialization;

namespace LineCom.CatalogImport.Core.Source;

public sealed record OneCExportDocument(
    [property: JsonPropertyName("source")] OneCExportSource Source,
    [property: JsonPropertyName("extraction")] OneCExportExtraction Extraction,
    [property: JsonPropertyName("categories")] IReadOnlyList<OneCExportCategory> Categories);

public sealed record OneCExportSource(
    [property: JsonPropertyName("file")] string? File,
    [property: JsonPropertyName("worksheet")] string? Worksheet,
    [property: JsonPropertyName("reportTitle")] string? ReportTitle);

public sealed record OneCExportExtraction(
    [property: JsonPropertyName("sourceAccount")] string SourceAccount,
    [property: JsonPropertyName("sourceAccountName")] string? SourceAccountName,
    [property: JsonPropertyName("itemCount")] int ItemCount,
    [property: JsonPropertyName("classificationBasis")] string? ClassificationBasis,
    [property: JsonPropertyName("importantNote")] string? ImportantNote);

public sealed record OneCExportCategory(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("projectCoreCategory")] bool ProjectCoreCategory,
    [property: JsonPropertyName("itemCount")] int ItemCount,
    [property: JsonPropertyName("items")] IReadOnlyList<OneCExportItem>? Items);

public sealed record OneCExportItem(
    [property: JsonPropertyName("sourceRow")] int SourceRow,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sourceAccount")] string SourceAccount,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("unitCost")] decimal? UnitCost,
    [property: JsonPropertyName("amount")] decimal? Amount,
    [property: JsonPropertyName("classification")] OneCExportClassification Classification);

public sealed record OneCExportClassification(
    [property: JsonPropertyName("categorySlug")] string CategorySlug,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("matchedKeywords")] IReadOnlyList<string> MatchedKeywords,
    [property: JsonPropertyName("needsReview")] bool NeedsReview);
