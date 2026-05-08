using System.Globalization;
using System.Text;
using System.Text.Json;
using LineCom.CatalogImport.Core.Planning;

namespace LineCom.CatalogImport.Core.Reporting;

public sealed record CatalogImportReportContext(
    string SourcePath,
    string? ImageManifestPath,
    string Mode,
    string? TargetDatabase);

public sealed record CatalogImportReportResult(string JsonPath, string MarkdownPath);

public static class CatalogImportReportWriter
{
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static CatalogImportReportResult WriteReports(
        CatalogImportPlan plan,
        string outputDirectory,
        CatalogImportReportContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(context);
        ValidateContext(context);

        Directory.CreateDirectory(outputDirectory);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var fileStamp = generatedAtUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var fileSuffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var jsonPath = Path.Combine(outputDirectory, $"catalog-import-{fileStamp}-{fileSuffix}.json");
        var markdownPath = Path.Combine(outputDirectory, $"catalog-import-{fileStamp}-{fileSuffix}.md");

        var report = new CatalogImportReport(
            SchemaVersion,
            generatedAtUtc,
            context,
            plan.Summary,
            plan.Categories,
            plan.Products,
            plan.Warnings);

        WriteAllTextCreateNew(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
        WriteAllTextCreateNew(markdownPath, WriteMarkdown(report));

        return new CatalogImportReportResult(jsonPath, markdownPath);
    }

    private static void ValidateContext(CatalogImportReportContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context.SourcePath, nameof(context.SourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(context.Mode, nameof(context.Mode));
    }

    private static void WriteAllTextCreateNew(string path, string contents)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(contents);
    }

    private static string WriteMarkdown(CatalogImportReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Catalog Import Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at UTC: {report.GeneratedAtUtc:O}");
        builder.AppendLine();
        builder.AppendLine("## Context");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Source path | {FormatInlineCodeTableCell(report.Context.SourcePath)} |");
        builder.AppendLine($"| Image manifest path | {FormatInlineCodeTableCell(report.Context.ImageManifestPath ?? "not provided")} |");
        builder.AppendLine($"| Import mode | {FormatTableCell(report.Context.Mode)} |");
        builder.AppendLine($"| Target database | {FormatTableCell(report.Context.TargetDatabase ?? "not specified")} |");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Count |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Categories | {report.Summary.Categories} |");
        builder.AppendLine($"| Products | {report.Summary.Products} |");
        builder.AppendLine($"| Published products | {report.Summary.PublishableProducts} |");
        builder.AppendLine($"| Draft products | {report.Summary.DraftProducts} |");
        builder.AppendLine($"| Image assignments | {report.Summary.ImageAssignments} |");
        builder.AppendLine($"| Warnings | {report.Summary.Warnings} |");
        builder.AppendLine();

        AppendImageAssignments(builder, report.Products);
        AppendWarnings(builder, report.Warnings);

        return builder.ToString();
    }

    private static void AppendImageAssignments(StringBuilder builder, IReadOnlyList<CatalogProductImportRow> products)
    {
        builder.AppendLine("## Image Assignments");
        builder.AppendLine();

        var assignedProducts = products.Where(product => product.Image is not null).ToArray();
        if (assignedProducts.Length == 0)
        {
            builder.AppendLine("No image assignments.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Source row | External ID | Product name | Asset key | File | Rights status |");
        builder.AppendLine("| ---: | --- | --- | --- | --- | --- |");
        foreach (var product in assignedProducts)
        {
            var image = product.Image!;
            builder.AppendLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"| {product.SourceRow} | {FormatTableCell(product.ExternalId)} | {FormatTableCell(product.Name)} | {FormatTableCell(image.AssetKey)} | {FormatTableCell(image.File)} | {FormatTableCell(image.RightsStatus)} |"));
        }

        builder.AppendLine();
    }

    private static void AppendWarnings(StringBuilder builder, IReadOnlyList<CatalogImportWarning> warnings)
    {
        builder.AppendLine("## Warnings");
        builder.AppendLine();

        if (warnings.Count == 0)
        {
            builder.AppendLine("No warnings.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Code | Source row | Message |");
        builder.AppendLine("| --- | ---: | --- |");
        foreach (var warning in warnings)
        {
            builder.AppendLine(
                $"| {FormatTableCell(warning.Code)} | {warning.SourceRow?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} | {FormatTableCell(warning.Message)} |");
        }

        builder.AppendLine();
    }

    private static string FormatTableCell(string value)
    {
        return NormalizeTableValue(value)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
    }

    private static string FormatInlineCodeTableCell(string value)
    {
        var escaped = NormalizeTableValue(value).Replace("|", "\\|", StringComparison.Ordinal);
        var delimiter = new string('`', GetLongestBacktickRun(escaped) + 1);
        if (escaped.Contains('`', StringComparison.Ordinal))
        {
            return $"{delimiter} {escaped} {delimiter}";
        }

        return $"{delimiter}{escaped}{delimiter}";
    }

    private static string NormalizeTableValue(string value)
    {
        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }

    private static int GetLongestBacktickRun(string value)
    {
        var longestRun = 0;
        var currentRun = 0;

        foreach (var character in value)
        {
            if (character == '`')
            {
                currentRun++;
                longestRun = Math.Max(longestRun, currentRun);
            }
            else
            {
                currentRun = 0;
            }
        }

        return longestRun;
    }

    private sealed record CatalogImportReport(
        int SchemaVersion,
        DateTimeOffset GeneratedAtUtc,
        CatalogImportReportContext Context,
        CatalogImportSummary Summary,
        IReadOnlyList<CatalogCategoryImportRow> Categories,
        IReadOnlyList<CatalogProductImportRow> Products,
        IReadOnlyList<CatalogImportWarning> Warnings);
}
