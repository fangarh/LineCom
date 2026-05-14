using System.Text.Json;
using LineCom.CatalogImport.Core.Database;
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Reporting;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportReportWriterTests
{
    [Fact]
    public void WriteReports_CreatesOperatorReviewJsonAndMarkdown()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var plan = CreatePlan();
        var context = new CatalogImportReportContext(
            SourcePath: @"D:\imports\catalog.xlsx",
            ImageManifestPath: @"D:\imports\images.csv",
            Mode: "dry-run",
            TargetDatabase: "linecom_local");

        var result = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);

        Assert.True(File.Exists(result.JsonPath));
        Assert.True(File.Exists(result.MarkdownPath));
        Assert.Equal(outputDirectory, Path.GetDirectoryName(result.JsonPath));
        Assert.Equal(outputDirectory, Path.GetDirectoryName(result.MarkdownPath));
        Assert.Matches(@"catalog-import-\d{8}T\d{6}Z-[0-9a-f]{32}\.json$", Path.GetFileName(result.JsonPath));
        Assert.Matches(@"catalog-import-\d{8}T\d{6}Z-[0-9a-f]{32}\.md$", Path.GetFileName(result.MarkdownPath));

        using var document = JsonDocument.Parse(File.ReadAllText(result.JsonPath));
        var root = document.RootElement;
        Assert.Equal(@"D:\imports\catalog.xlsx", root.GetProperty("context").GetProperty("sourcePath").GetString());
        Assert.Equal(@"D:\imports\images.csv", root.GetProperty("context").GetProperty("imageManifestPath").GetString());
        Assert.Equal("dry-run", root.GetProperty("context").GetProperty("mode").GetString());
        Assert.Equal("linecom_local", root.GetProperty("context").GetProperty("targetDatabase").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("categories").GetInt32());
        Assert.Equal(3, root.GetProperty("summary").GetProperty("products").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("publishableProducts").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("draftProducts").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("imageAssignments").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("warnings").GetInt32());
        Assert.True(root.TryGetProperty("generatedAtUtc", out var generatedAtUtc));
        Assert.True(DateTimeOffset.TryParse(generatedAtUtc.GetString(), out _));
        Assert.Equal(2, root.GetProperty("categories").GetArrayLength());
        Assert.Equal(3, root.GetProperty("products").GetArrayLength());
        Assert.Equal(1, root.GetProperty("warnings").GetArrayLength());

        var json = File.ReadAllText(result.JsonPath);
        Assert.Contains(Environment.NewLine + "  \"generatedAtUtc\"", json);

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("# Catalog Import Report", markdown);
        Assert.Contains("| Source path | `D:\\imports\\catalog.xlsx` |", markdown);
        Assert.Contains("| Image manifest path | `D:\\imports\\images.csv` |", markdown);
        Assert.Contains("| Import mode | dry-run |", markdown);
        Assert.Contains("| Target database | linecom_local |", markdown);
        Assert.Contains("| Categories | 2 |", markdown);
        Assert.Contains("| Products | 3 |", markdown);
        Assert.Contains("| Published products | 1 |", markdown);
        Assert.Contains("| Draft products | 2 |", markdown);
        Assert.Contains("| Image assignments | 1 |", markdown);
        Assert.Contains("| Warnings | 1 |", markdown);
        Assert.Contains("## Image Assignments", markdown);
        Assert.Contains("| 10 | ext-10 | Cable A | asset-10 | images/cable-a.png | requires-permission |", markdown);
        Assert.Contains("## Warnings", markdown);
        Assert.Contains("| product.requires_review | 11 | Missing category |", markdown);
    }

    [Fact]
    public void WriteReports_CreatesUniqueFilesForRapidCallsInSameDirectory()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var plan = CreatePlan();
        var context = CreateContext();

        var first = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);
        var second = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);

        Assert.NotEqual(first.JsonPath, second.JsonPath);
        Assert.NotEqual(first.MarkdownPath, second.MarkdownPath);
        Assert.True(File.Exists(first.JsonPath));
        Assert.True(File.Exists(first.MarkdownPath));
        Assert.True(File.Exists(second.JsonPath));
        Assert.True(File.Exists(second.MarkdownPath));
    }

    [Fact]
    public void WriteReports_EscapesMarkdownTableValuesFromExternalInput()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var plan = CreatePlan(
            productName: "Cable | A\r\n`special`",
            imageFile: "images/cable|a\r\n`front`.png",
            warningMessage: "Missing | category\r\n`review`");
        var context = new CatalogImportReportContext(
            SourcePath: "D:\\imports\\catalog|broken\r\n`name`.xlsx",
            ImageManifestPath: "D:\\imports\\images|broken\r\n`manifest`.csv",
            Mode: "dry|run",
            TargetDatabase: "linecom|local");

        var result = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("| Source path | `` D:\\imports\\catalog\\|broken `name`.xlsx `` |", markdown);
        Assert.Contains("| Image manifest path | `` D:\\imports\\images\\|broken `manifest`.csv `` |", markdown);
        Assert.Contains("| Import mode | dry\\|run |", markdown);
        Assert.Contains("| Target database | linecom\\|local |", markdown);
        Assert.Contains("| 10 | ext-10 | Cable \\| A \\`special\\` | asset-10 | images/cable\\|a \\`front\\`.png | requires-permission |", markdown);
        Assert.Contains("| product.requires_review | 11 | Missing \\| category \\`review\\` |", markdown);
        Assert.DoesNotContain("broken\r\n", markdown);
        Assert.DoesNotContain("A\r\n", markdown);
    }

    [Fact]
    public void WriteReports_UsesInlineCodeDelimiterLongerThanExternalBacktickRuns()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var plan = CreatePlan();
        var context = new CatalogImportReportContext(
            SourcePath: "D:\\imports\\catalog|broken\r\n``name``.xlsx",
            ImageManifestPath: "D:\\imports\\images|broken\r\n``manifest``.csv",
            Mode: "dry-run",
            TargetDatabase: null);

        var result = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("| Source path | ``` D:\\imports\\catalog\\|broken ``name``.xlsx ``` |", markdown);
        Assert.Contains("| Image manifest path | ``` D:\\imports\\images\\|broken ``manifest``.csv ``` |", markdown);
        Assert.DoesNotContain("broken\r\n", markdown);
    }

    [Theory]
    [InlineData(null, "dry-run", "SourcePath")]
    [InlineData("", "dry-run", "SourcePath")]
    [InlineData("   ", "dry-run", "SourcePath")]
    [InlineData("source.xlsx", null, "Mode")]
    [InlineData("source.xlsx", "", "Mode")]
    [InlineData("source.xlsx", "   ", "Mode")]
    public void WriteReports_RejectsMissingRequiredContextValues(string? sourcePath, string? mode, string expectedParameterName)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var context = new CatalogImportReportContext(sourcePath!, null, mode!, null);

        var exception = Assert.ThrowsAny<ArgumentException>(() => CatalogImportReportWriter.WriteReports(CreatePlan(), outputDirectory, context));

        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    [Fact]
    public void WriteReports_UsesFallbackTextForOptionalContextAndEmptyDetails()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var plan = CreatePlan(includeImage: false, includeWarning: false);
        var context = new CatalogImportReportContext("source.xlsx", null, "dry-run", null);

        var result = CatalogImportReportWriter.WriteReports(plan, outputDirectory, context);

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("| Image manifest path | `not provided` |", markdown);
        Assert.Contains("| Target database | not specified |", markdown);
        Assert.Contains("No image assignments.", markdown);
        Assert.Contains("No warnings.", markdown);
    }

    [Fact]
    public void WriteReports_IncludesStorageLifecycleOutcomesWithoutAbsoluteStoragePaths()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "linecom-catalog-import-report-tests", Guid.NewGuid().ToString("N"));
        var storageRootPath = Path.Combine(Path.GetTempPath(), "linecom-storage-root", Guid.NewGuid().ToString("N"));
        var applyResult = new CatalogImportApplyResult(
            CategoriesProcessed: 2,
            ProductsProcessed: 3,
            ImagesProcessed: 1,
            Storage: new CatalogImportApplyStorageResult(
                RunId: "run-1",
                StagedFiles: 2,
                PromotedFiles: 1,
                PromotionFailures: [new CatalogImportStorageOperationFailure("storage/products/catalog-import/a.png", "locked")],
                CleanupFailures: [new CatalogImportStorageOperationFailure(".staging/catalog-import/run-1/a.png", "cleanup failed")],
                OldStagingLeftovers: [".staging/catalog-import/old-run"]),
            ResetStorageCleanup: new CatalogImportResetStorageCleanupResult(
                SelectedFiles: 3,
                DeletedFiles: 2,
                Failures: [new CatalogImportStorageOperationFailure("storage/products/catalog-import/b.png", "locked")],
                UntrackedLeftovers: ["storage/products/catalog-import/untracked.png"]));
        var context = new CatalogImportReportContext(
            SourcePath: "source.xlsx",
            ImageManifestPath: "images.json",
            Mode: "reset-apply",
            TargetDatabase: "configured",
            ApplyResult: applyResult);

        var result = CatalogImportReportWriter.WriteReports(CreatePlan(), outputDirectory, context);

        using var document = JsonDocument.Parse(File.ReadAllText(result.JsonPath));
        var root = document.RootElement;
        var storage = root.GetProperty("context").GetProperty("applyResult").GetProperty("storage");
        Assert.Equal("run-1", storage.GetProperty("runId").GetString());
        Assert.Equal(2, storage.GetProperty("stagedFiles").GetInt32());
        Assert.Equal(1, storage.GetProperty("promotedFiles").GetInt32());
        Assert.Equal("storage/products/catalog-import/a.png", storage.GetProperty("promotionFailures")[0].GetProperty("key").GetString());

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("## Storage Lifecycle", markdown);
        Assert.Contains("| Run ID | run-1 |", markdown);
        Assert.Contains("| Staged files | 2 |", markdown);
        Assert.Contains("| Promoted files | 1 |", markdown);
        Assert.Contains("| storage/products/catalog-import/a.png | locked |", markdown);
        Assert.Contains("| .staging/catalog-import/old-run |", markdown);
        Assert.Contains("### Reset Storage Cleanup", markdown);
        Assert.Contains("| Selected files | 3 |", markdown);
        Assert.Contains("| Deleted files | 2 |", markdown);
        Assert.Contains("| storage/products/catalog-import/untracked.png |", markdown);
        Assert.DoesNotContain(storageRootPath, markdown);
        Assert.DoesNotContain(storageRootPath, File.ReadAllText(result.JsonPath));
    }

    private static CatalogImportReportContext CreateContext()
    {
        return new CatalogImportReportContext(
            SourcePath: @"D:\imports\catalog.xlsx",
            ImageManifestPath: @"D:\imports\images.csv",
            Mode: "dry-run",
            TargetDatabase: "linecom_local");
    }

    private static CatalogImportPlan CreatePlan(
        string productName = "Cable A",
        string imageFile = "images/cable-a.png",
        string warningMessage = "Missing category",
        bool includeImage = true,
        bool includeWarning = true)
    {
        var categories = new[]
        {
            new CatalogCategoryImportRow("cables", "Cables", 0, true, true),
            new CatalogCategoryImportRow("tools", "Tools", 1, true, false)
        };
        var products = new[]
        {
            new CatalogProductImportRow(
                SourceRow: 10,
                ExternalId: "ext-10",
                Name: productName,
                Slug: "cable-a",
                CategorySlug: "cables",
                AvailabilityStatus: "check_availability",
                SaleUnit: "piece",
                UnitQuantity: "1 pc.",
                PublishStatus: "published",
                SortOrder: 0,
                Image: includeImage ? new CatalogProductImageImportRow("asset-10", imageFile, "requires-permission") : null,
                Attributes: []),
            new CatalogProductImportRow(
                SourceRow: 11,
                ExternalId: "ext-11",
                Name: "Cable B",
                Slug: "cable-b",
                CategorySlug: "cables",
                AvailabilityStatus: "check_availability",
                SaleUnit: "piece",
                UnitQuantity: "1 pc.",
                PublishStatus: "draft",
                SortOrder: 1,
                Image: null,
                Attributes: []),
            new CatalogProductImportRow(
                SourceRow: 12,
                ExternalId: "ext-12",
                Name: "Tool A",
                Slug: "tool-a",
                CategorySlug: "tools",
                AvailabilityStatus: "check_availability",
                SaleUnit: "piece",
                UnitQuantity: "1 pc.",
                PublishStatus: "draft",
                SortOrder: 2,
                Image: null,
                Attributes: [])
        };
        var warnings = includeWarning
            ? [new CatalogImportWarning(11, "product.requires_review", warningMessage)]
            : Array.Empty<CatalogImportWarning>();
        var summary = new CatalogImportSummary(
            Categories: 2,
            Products: 3,
            PublishableProducts: 1,
            DraftProducts: 2,
            ImageAssignments: includeImage ? 1 : 0,
            Warnings: warnings.Length);

        return new CatalogImportPlan(summary, categories, products, warnings);
    }
}
