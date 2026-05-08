# Catalog Importer WinForms Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first production-oriented catalog importer with a WinForms operator UI, dry-run preview, guarded dev/QA reset, database apply, and reports.

**Architecture:** Add a UI-independent `LineCom.CatalogImport.Core` library that owns parsing, planning, validation, database import, reset safety, and reports. Add a `LineCom.CatalogImport.WinForms` app as a thin desktop shell over that core. Keep importer rules testable without UI automation.

**Tech Stack:** .NET 8, WinForms, Npgsql, Dapper, xUnit, PostgreSQL catalog tables, local file storage, JSON source files.

---

## Source Context

- Design spec: `docs/superpowers/specs/2026-05-08-catalog-importer-winforms-design.md`
- Source JSON: `Assets/1c_export_41_01_nomenclature_by_category.json`
- Reviewed image manifest: `Assets/product-images/part1_png_reviewed_manifest.json`
- Catalog schema: `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
- Shared image migration: `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`
- Public catalog API docs: `vault/Человекочитаемое/Public Catalog API.md`
- Catalog import notes: `vault/Человекочитаемое/Catalog Image Import iterations.md`

Do not import public prices, payment semantics, online order flow, or public stock quantities.

## File Structure

- Create: `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`
  - Core library for source parsing, planning, reporting, and database import.
- Create: `apps/catalog-import.core/Source/OneCExportModels.cs`
  - DTOs for the normalized 1C JSON.
- Create: `apps/catalog-import.core/Source/OneCExportReader.cs`
  - Reads and validates source JSON.
- Create: `apps/catalog-import.core/Planning/CatalogImportModels.cs`
  - Import plan, row, summary, warning, and error records.
- Create: `apps/catalog-import.core/Planning/SlugGenerator.cs`
  - Deterministic ASCII slug generation with collision suffixes.
- Create: `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`
  - Converts source JSON into dry-run import plan.
- Create: `apps/catalog-import.core/Images/ProductImageManifestModels.cs`
  - DTOs for reviewed image manifest.
- Create: `apps/catalog-import.core/Images/ProductImageManifestReader.cs`
  - Reads accepted image assignments by source row.
- Create: `apps/catalog-import.core/Reporting/CatalogImportReportWriter.cs`
  - Writes JSON and Markdown reports.
- Create: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`
  - Applies upsert/reset logic to PostgreSQL through Npgsql/Dapper.
- Create: `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`
  - WinForms app project.
- Create: `apps/catalog-import.winforms/Program.cs`
  - WinForms entrypoint.
- Create: `apps/catalog-import.winforms/MainForm.cs`
  - Wizard-like form for source selection, dry-run, apply, and report display.
- Modify: `LineCom.sln`
  - Add both importer projects.
- Modify: `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`
  - Add project reference to `LineCom.CatalogImport.Core`.
- Create: `tests/LineCom.Api.Tests/CatalogImport/OneCExportReaderTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/SlugGeneratorTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportPlannerTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/ProductImageManifestReaderTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportReportWriterTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportDatabaseSqlTests.cs`
- Modify: `vault/Человекочитаемое/Catalog Image Import iterations.md`
  - Record the importer implementation, commands, and first dry-run/apply results.

## Iteration Breakdown

Run these iterations one at a time. After each iteration, stop, review the result, and only then start the next one.

### Iteration 1: Core Project And Source Reader

Goal: add the core project and prove the normalized 1C JSON can be parsed and validated.

Do:

- Task 1 only.

Stop after:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
dotnet build LineCom.sln -m:1
```

### Iteration 2: Dry-Run Planner And Image Manifest Mapping

Goal: build a deterministic dry-run plan from the 1C JSON and reviewed image manifest.

Do:

- Task 2 only.

Stop after:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
dotnet build LineCom.sln -m:1
```

### Iteration 3: Reports

Goal: write JSON and Markdown dry-run reports that are useful for operator/customer review.

Do:

- Task 3 only.

Stop after:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
dotnet build LineCom.sln -m:1
```

### Iteration 4: Database Apply And Reset Safety

Goal: implement dev/QA guarded reset checks and catalog upsert SQL.

Do:

- Task 4 only.

Stop after:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
dotnet build LineCom.sln -m:1
```

### Iteration 5: WinForms Shell

Goal: add a working WinForms app that can select files, run dry-run, show preview, and call apply.

Do:

- Task 5 only.

Stop after:

```powershell
dotnet build LineCom.sln -m:1
```

### Iteration 6: Documentation And Full Verification

Goal: document the pipeline and run full verification.

Do:

- Task 6 only.

Stop after:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
npm.cmd test
npm.cmd run build
```

Run frontend commands from `apps/front`.

## Task 1: Core Project And Source Reader

**Files:**

- Create: `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`
- Create: `apps/catalog-import.core/Source/OneCExportModels.cs`
- Create: `apps/catalog-import.core/Source/OneCExportReader.cs`
- Modify: `LineCom.sln`
- Modify: `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`
- Create: `tests/LineCom.Api.Tests/CatalogImport/OneCExportReaderTests.cs`

- [ ] **Step 1: Create the core project file**

Create `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Npgsql" Version="8.0.6" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run:

```powershell
dotnet sln LineCom.sln add apps\catalog-import.core\LineCom.CatalogImport.Core.csproj
```

Expected: solution includes `LineCom.CatalogImport.Core`.

- [ ] **Step 3: Add test project reference**

Modify `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj` and add:

```xml
<ProjectReference Include="..\..\apps\catalog-import.core\LineCom.CatalogImport.Core.csproj" />
```

inside the existing `<ItemGroup>` with project references.

- [ ] **Step 4: Write failing reader tests**

Create `tests/LineCom.Api.Tests/CatalogImport/OneCExportReaderTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Source;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class OneCExportReaderTests
{
    [Fact]
    public void Read_LoadsNormalizedOneCExport()
    {
        var sourcePath = Path.Combine(FindRepositoryRoot(), "Assets", "1c_export_41_01_nomenclature_by_category.json");

        var export = OneCExportReader.Read(sourcePath);

        Assert.Equal("41.01", export.Extraction.SourceAccount);
        Assert.True(export.Extraction.ItemCount > 0);
        Assert.NotEmpty(export.Categories);
        Assert.Contains(export.Categories, category => category.Slug == "twisted-pair-cable");
        Assert.All(export.Categories, category => Assert.False(string.IsNullOrWhiteSpace(category.Name)));
    }

    [Fact]
    public void Read_ThrowsClearError_WhenItemsAreMissing()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              },
              "categories": [
                {
                  "slug": "broken",
                  "name": "Broken"
                }
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Category 'broken' does not contain an items array.", exception.Message);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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
```

- [ ] **Step 5: Run tests and verify they fail**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter OneCExportReaderTests
```

Expected: fail because `LineCom.CatalogImport.Core.Source` does not exist.

- [ ] **Step 6: Add source DTOs**

Create `apps/catalog-import.core/Source/OneCExportModels.cs`:

```csharp
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
```

- [ ] **Step 7: Add source reader**

Create `apps/catalog-import.core/Source/OneCExportReader.cs`:

```csharp
using System.Text.Json;

namespace LineCom.CatalogImport.Core.Source;

public static class OneCExportReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static OneCExportDocument Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Source path is required.", nameof(path));
        }

        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<OneCExportDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException("1C export JSON is empty or invalid.");

        Validate(document);

        return document;
    }

    private static void Validate(OneCExportDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Extraction.SourceAccount))
        {
            throw new InvalidOperationException("Extraction sourceAccount is required.");
        }

        if (document.Categories.Count == 0)
        {
            throw new InvalidOperationException("At least one category is required.");
        }

        foreach (var category in document.Categories)
        {
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                throw new InvalidOperationException("Category slug is required.");
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new InvalidOperationException($"Category '{category.Slug}' name is required.");
            }

            if (category.Items is null)
            {
                throw new InvalidOperationException($"Category '{category.Slug}' does not contain an items array.");
            }

            foreach (var item in category.Items)
            {
                if (item.SourceRow <= 0)
                {
                    throw new InvalidOperationException($"Category '{category.Slug}' contains an item without sourceRow.");
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new InvalidOperationException($"Item at source row {item.SourceRow} has empty name.");
                }
            }
        }
    }
}
```

- [ ] **Step 8: Run tests and build**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter OneCExportReaderTests
dotnet build LineCom.sln -m:1
```

Expected: tests and build pass.

- [ ] **Step 9: Commit**

Run:

```powershell
git add apps/catalog-import.core tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj tests/LineCom.Api.Tests/CatalogImport/OneCExportReaderTests.cs LineCom.sln
git commit -m "feat: add catalog import source reader"
```

## Task 2: Dry-Run Planner And Image Manifest Mapping

**Files:**

- Create: `apps/catalog-import.core/Planning/CatalogImportModels.cs`
- Create: `apps/catalog-import.core/Planning/SlugGenerator.cs`
- Create: `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`
- Create: `apps/catalog-import.core/Images/ProductImageManifestModels.cs`
- Create: `apps/catalog-import.core/Images/ProductImageManifestReader.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/SlugGeneratorTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportPlannerTests.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/ProductImageManifestReaderTests.cs`

- [ ] **Step 1: Write failing slug tests**

Create `tests/LineCom.Api.Tests/CatalogImport/SlugGeneratorTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Planning;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class SlugGeneratorTests
{
    [Fact]
    public void CreateUniqueSlug_TransliteratesRussianAndKeepsTechnicalTokens()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var slug = SlugGenerator.CreateUniqueSlug("Кабель LANMAX UTP4 cat.5e, 305m, Cu", used);

        Assert.Equal("kabel-lanmax-utp4-cat-5e-305m-cu", slug);
    }

    [Fact]
    public void CreateUniqueSlug_AppendsSuffixForCollisions()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kabel-lanmax-utp4-cat-5e",
            "kabel-lanmax-utp4-cat-5e-2"
        };

        var slug = SlugGenerator.CreateUniqueSlug("Кабель LANMAX UTP4 cat.5e", used);

        Assert.Equal("kabel-lanmax-utp4-cat-5e-3", slug);
    }
}
```

- [ ] **Step 2: Write failing image manifest tests**

Create `tests/LineCom.Api.Tests/CatalogImport/ProductImageManifestReaderTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ProductImageManifestReaderTests
{
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
                }
              ]
            }
            """);

        var images = ProductImageManifestReader.ReadAcceptedBySourceRow(manifest);

        Assert.True(images.ContainsKey(10));
        Assert.True(images.ContainsKey(11));
        Assert.False(images.ContainsKey(12));
        Assert.Equal("accepted", images[10].AssetKey);
        Assert.Equal("Assets/product-images/accepted.png", images[10].File);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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
```

- [ ] **Step 3: Write failing planner tests**

Create `tests/LineCom.Api.Tests/CatalogImport/CatalogImportPlannerTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportPlannerTests
{
    [Fact]
    public void BuildPlan_CreatesCategoriesProductsAndImageAssignments()
    {
        var document = new OneCExportDocument(
            new OneCExportSource(null, null, null),
            new OneCExportExtraction("41.01", "Товары на складах", 2, null, null),
            [
                new OneCExportCategory(
                    "twisted-pair-cable",
                    "Витая пара",
                    true,
                    2,
                    [
                        CreateItem(106, "Кабель LANMAX UTP4 cat.5e, 305m, Cu", "high", needsReview: false),
                        CreateItem(107, "Неизвестная позиция", "low", needsReview: true)
                    ])
            ]);
        var images = new Dictionary<int, ProductImageManifestItem>
        {
            [106] = new("lanmax-utp", "Assets/product-images/lanmax.png", "requires-permission")
        };

        var plan = CatalogImportPlanner.BuildPlan(document, images);

        Assert.Single(plan.Categories);
        Assert.Equal(2, plan.Products.Count);
        Assert.Equal("1c:41.01:row:106", plan.Products[0].ExternalId);
        Assert.Equal("published", plan.Products[0].PublishStatus);
        Assert.Equal("draft", plan.Products[1].PublishStatus);
        Assert.Equal("check_availability", plan.Products[0].AvailabilityStatus);
        Assert.Equal("piece", plan.Products[0].SaleUnit);
        Assert.Equal("1 шт.", plan.Products[0].UnitQuantity);
        Assert.Equal("lanmax-utp", plan.Products[0].Image?.AssetKey);
        Assert.Equal(1, plan.Summary.PublishableProducts);
        Assert.Equal(1, plan.Summary.DraftProducts);
        Assert.Equal(1, plan.Summary.ImageAssignments);
    }

    private static OneCExportItem CreateItem(int sourceRow, string name, string confidence, bool needsReview)
    {
        return new OneCExportItem(
            sourceRow,
            name,
            "41.01",
            Quantity: 1,
            UnitCost: null,
            Amount: null,
            new OneCExportClassification(
                "twisted-pair-cable",
                "Витая пара",
                confidence,
                [],
                needsReview));
    }
}
```

- [ ] **Step 4: Run tests and verify they fail**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
```

Expected: fail because planning/image classes do not exist.

- [ ] **Step 5: Add image manifest DTOs and reader**

Create `apps/catalog-import.core/Images/ProductImageManifestModels.cs`:

```csharp
using System.Text.Json.Serialization;

namespace LineCom.CatalogImport.Core.Images;

internal sealed record ProductImageManifestDocument(
    [property: JsonPropertyName("items")] IReadOnlyList<ProductImageManifestItemRaw> Items);

internal sealed record ProductImageManifestItemRaw(
    [property: JsonPropertyName("assetKey")] string AssetKey,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("file")] string? File,
    [property: JsonPropertyName("sourceRows")] IReadOnlyList<int>? SourceRows,
    [property: JsonPropertyName("visualReviewStatus")] string? VisualReviewStatus,
    [property: JsonPropertyName("rightsStatus")] string? RightsStatus);

public sealed record ProductImageManifestItem(
    string AssetKey,
    string File,
    string RightsStatus);
```

Create `apps/catalog-import.core/Images/ProductImageManifestReader.cs`:

```csharp
using System.Text.Json;

namespace LineCom.CatalogImport.Core.Images;

public static class ProductImageManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyDictionary<int, ProductImageManifestItem> ReadAcceptedBySourceRow(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new Dictionary<int, ProductImageManifestItem>();
        }

        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<ProductImageManifestDocument>(stream, JsonOptions)
            ?? new ProductImageManifestDocument([]);

        var result = new Dictionary<int, ProductImageManifestItem>();
        foreach (var item in document.Items)
        {
            if (!string.Equals(item.Status, "downloaded_png", StringComparison.Ordinal) ||
                !string.Equals(item.VisualReviewStatus, "accepted_visual_scan", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(item.File) ||
                item.SourceRows is null)
            {
                continue;
            }

            var accepted = new ProductImageManifestItem(
                item.AssetKey,
                item.File,
                string.IsNullOrWhiteSpace(item.RightsStatus) ? "requires-permission" : item.RightsStatus);

            foreach (var sourceRow in item.SourceRows)
            {
                result.TryAdd(sourceRow, accepted);
            }
        }

        return result;
    }
}
```

- [ ] **Step 6: Add planning models**

Create `apps/catalog-import.core/Planning/CatalogImportModels.cs`:

```csharp
namespace LineCom.CatalogImport.Core.Planning;

public sealed record CatalogImportPlan(
    CatalogImportSummary Summary,
    IReadOnlyList<CatalogCategoryImportRow> Categories,
    IReadOnlyList<CatalogProductImportRow> Products,
    IReadOnlyList<CatalogImportWarning> Warnings);

public sealed record CatalogImportSummary(
    int Categories,
    int Products,
    int PublishableProducts,
    int DraftProducts,
    int ImageAssignments,
    int Warnings);

public sealed record CatalogCategoryImportRow(
    string Slug,
    string Name,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu);

public sealed record CatalogProductImportRow(
    int SourceRow,
    string ExternalId,
    string Name,
    string Slug,
    string CategorySlug,
    string AvailabilityStatus,
    string SaleUnit,
    string UnitQuantity,
    string PublishStatus,
    int SortOrder,
    CatalogProductImageImportRow? Image);

public sealed record CatalogProductImageImportRow(
    string AssetKey,
    string File,
    string RightsStatus);

public sealed record CatalogImportWarning(
    int? SourceRow,
    string Code,
    string Message);
```

- [ ] **Step 7: Add slug generator**

Create `apps/catalog-import.core/Planning/SlugGenerator.cs`:

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace LineCom.CatalogImport.Core.Planning;

public static partial class SlugGenerator
{
    private static readonly IReadOnlyDictionary<char, string> Transliteration = new Dictionary<char, string>
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ё'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
        ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
        ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
        ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch",
        ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
        ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
    };

    public static string CreateUniqueSlug(string value, ISet<string> usedSlugs)
    {
        var baseSlug = CreateSlug(value);
        var candidate = baseSlug;
        var suffix = 2;

        while (!usedSlugs.Add(candidate))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string CreateSlug(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (Transliteration.TryGetValue(character, out var replacement))
            {
                builder.Append(replacement);
            }
            else if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('-');
            }
        }

        var slug = NonSlugCharacters().Replace(builder.ToString(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "product" : slug;
    }

    [GeneratedRegex("-+")]
    private static partial Regex NonSlugCharacters();
}
```

- [ ] **Step 8: Add planner**

Create `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`:

```csharp
using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.CatalogImport.Core.Planning;

public static class CatalogImportPlanner
{
    public static CatalogImportPlan BuildPlan(
        OneCExportDocument source,
        IReadOnlyDictionary<int, ProductImageManifestItem> imagesBySourceRow)
    {
        var categories = source.Categories
            .Select((category, index) => new CatalogCategoryImportRow(
                category.Slug,
                category.Name,
                index,
                IsActive: true,
                IsVisibleInMenu: true))
            .ToArray();

        var products = new List<CatalogProductImportRow>();
        var warnings = new List<CatalogImportWarning>();
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 0;

        foreach (var category in source.Categories)
        {
            foreach (var item in category.Items ?? [])
            {
                var publishStatus = IsPublishable(item) ? "published" : "draft";
                if (publishStatus == "draft")
                {
                    warnings.Add(new CatalogImportWarning(
                        item.SourceRow,
                        "product.requires_review",
                        $"Product at source row {item.SourceRow} is imported as draft."));
                }

                imagesBySourceRow.TryGetValue(item.SourceRow, out var image);

                products.Add(new CatalogProductImportRow(
                    item.SourceRow,
                    $"1c:{source.Extraction.SourceAccount}:row:{item.SourceRow}",
                    item.Name,
                    SlugGenerator.CreateUniqueSlug(item.Name, usedSlugs),
                    category.Slug,
                    "check_availability",
                    "piece",
                    "1 шт.",
                    publishStatus,
                    sortOrder,
                    image is null ? null : new CatalogProductImageImportRow(image.AssetKey, image.File, image.RightsStatus)));

                sortOrder++;
            }
        }

        var publishable = products.Count(product => product.PublishStatus == "published");
        var draft = products.Count - publishable;
        var imageAssignments = products.Count(product => product.Image is not null);

        return new CatalogImportPlan(
            new CatalogImportSummary(
                categories.Length,
                products.Count,
                publishable,
                draft,
                imageAssignments,
                warnings.Count),
            categories,
            products,
            warnings);
    }

    private static bool IsPublishable(OneCExportItem item)
    {
        return string.Equals(item.Classification.Confidence, "high", StringComparison.OrdinalIgnoreCase) &&
            !item.Classification.NeedsReview;
    }
}
```

- [ ] **Step 9: Run tests and build**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImport
dotnet build LineCom.sln -m:1
```

Expected: tests and build pass.

- [ ] **Step 10: Commit**

Run:

```powershell
git add apps/catalog-import.core tests/LineCom.Api.Tests/CatalogImport
git commit -m "feat: add catalog import dry run planner"
```

## Task 3: Reports

**Files:**

- Create: `apps/catalog-import.core/Reporting/CatalogImportReportWriter.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportReportWriterTests.cs`

- [ ] **Step 1: Write failing report tests**

Create `tests/LineCom.Api.Tests/CatalogImport/CatalogImportReportWriterTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Reporting;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportReportWriterTests
{
    [Fact]
    public void WriteReports_CreatesJsonAndMarkdownReports()
    {
        using var temp = new TemporaryDirectory();
        var plan = new CatalogImportPlan(
            new CatalogImportSummary(1, 1, 1, 0, 1, 0),
            [new CatalogCategoryImportRow("twisted-pair-cable", "Витая пара", 0, true, true)],
            [
                new CatalogProductImportRow(
                    106,
                    "1c:41.01:row:106",
                    "Кабель LANMAX",
                    "kabel-lanmax",
                    "twisted-pair-cable",
                    "check_availability",
                    "piece",
                    "1 шт.",
                    "published",
                    0,
                    new CatalogProductImageImportRow("lanmax", "Assets/product-images/lanmax.png", "requires-permission"))
            ],
            []);

        var result = CatalogImportReportWriter.WriteReports(
            plan,
            temp.Path,
            new CatalogImportReportContext(
                "source.json",
                "manifest.json",
                "dry-run",
                "LineCom_QA"));

        Assert.True(File.Exists(result.JsonPath));
        Assert.True(File.Exists(result.MarkdownPath));

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.Contains("# Catalog Import Report", markdown);
        Assert.Contains("Products: 1", markdown);
        Assert.Contains("Published: 1", markdown);
        Assert.Contains("LineCom_QA", markdown);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImportReportWriterTests
```

Expected: fail because `CatalogImportReportWriter` does not exist.

- [ ] **Step 3: Add report writer**

Create `apps/catalog-import.core/Reporting/CatalogImportReportWriter.cs`:

```csharp
using System.Text;
using System.Text.Json;
using LineCom.CatalogImport.Core.Planning;

namespace LineCom.CatalogImport.Core.Reporting;

public sealed record CatalogImportReportContext(
    string SourcePath,
    string? ImageManifestPath,
    string Mode,
    string? TargetDatabase);

public sealed record CatalogImportReportResult(
    string JsonPath,
    string MarkdownPath);

public static class CatalogImportReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static CatalogImportReportResult WriteReports(
        CatalogImportPlan plan,
        string outputDirectory,
        CatalogImportReportContext context)
    {
        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var jsonPath = Path.Combine(outputDirectory, $"catalog-import-{timestamp}.json");
        var markdownPath = Path.Combine(outputDirectory, $"catalog-import-{timestamp}.md");

        var payload = new
        {
            context,
            plan.Summary,
            plan.Categories,
            plan.Products,
            plan.Warnings
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);
        File.WriteAllText(markdownPath, BuildMarkdown(plan, context), Encoding.UTF8);

        return new CatalogImportReportResult(jsonPath, markdownPath);
    }

    private static string BuildMarkdown(CatalogImportPlan plan, CatalogImportReportContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Catalog Import Report");
        builder.AppendLine();
        builder.AppendLine($"Mode: {context.Mode}");
        builder.AppendLine($"Source: `{context.SourcePath}`");
        builder.AppendLine($"Image manifest: `{context.ImageManifestPath ?? "not selected"}`");
        builder.AppendLine($"Target database: `{context.TargetDatabase ?? "not recorded"}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"Categories: {plan.Summary.Categories}");
        builder.AppendLine($"Products: {plan.Summary.Products}");
        builder.AppendLine($"Published: {plan.Summary.PublishableProducts}");
        builder.AppendLine($"Draft: {plan.Summary.DraftProducts}");
        builder.AppendLine($"Image assignments: {plan.Summary.ImageAssignments}");
        builder.AppendLine($"Warnings: {plan.Summary.Warnings}");

        if (plan.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Warnings");
            builder.AppendLine();
            foreach (var warning in plan.Warnings)
            {
                builder.AppendLine($"- `{warning.Code}` row `{warning.SourceRow?.ToString() ?? "-"}`: {warning.Message}");
            }
        }

        return builder.ToString();
    }
}
```

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImportReportWriterTests
dotnet build LineCom.sln -m:1
```

Expected: tests and build pass.

- [ ] **Step 5: Commit**

Run:

```powershell
git add apps/catalog-import.core/Reporting tests/LineCom.Api.Tests/CatalogImport/CatalogImportReportWriterTests.cs
git commit -m "feat: add catalog import reports"
```

## Task 4: Database Apply And Reset Safety

**Files:**

- Create: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`
- Create: `tests/LineCom.Api.Tests/CatalogImport/CatalogImportDatabaseSqlTests.cs`

- [ ] **Step 1: Write failing database SQL tests**

Create `tests/LineCom.Api.Tests/CatalogImport/CatalogImportDatabaseSqlTests.cs`:

```csharp
using LineCom.CatalogImport.Core.Database;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportDatabaseSqlTests
{
    [Fact]
    public void ResetSql_DoesNotDeleteCustomerRequests()
    {
        Assert.DoesNotContain("customer_requests", CatalogImportDatabaseSql.ResetCatalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("request_items", CatalogImportDatabaseSql.ResetCatalog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProtectedReferenceSql_ChecksRequestItems()
    {
        Assert.Contains("request_items", CatalogImportDatabaseSql.CountProtectedProductReferences, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("products", CatalogImportDatabaseSql.CountProtectedProductReferences, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpsertProductSql_UsesExternalIdConflict()
    {
        Assert.Contains("ON CONFLICT (external_id)", CatalogImportDatabaseSql.UpsertProduct, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("price", CatalogImportDatabaseSql.UpsertProduct, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImportDatabaseSqlTests
```

Expected: fail because database import classes do not exist.

- [ ] **Step 3: Add database SQL and importer skeleton**

Create `apps/catalog-import.core/Database/CatalogImportDatabase.cs`:

```csharp
using Dapper;
using LineCom.CatalogImport.Core.Planning;
using Npgsql;

namespace LineCom.CatalogImport.Core.Database;

public static class CatalogImportDatabaseSql
{
    public const string CountProtectedProductReferences = """
        SELECT COUNT(*)
        FROM request_items item
        INNER JOIN products product ON product.id = item.product_id;
        """;

    public const string ResetCatalog = """
        DELETE FROM product_images;
        DELETE FROM product_attribute_values;
        DELETE FROM attribute_value_aliases;
        DELETE FROM attribute_options;
        DELETE FROM category_attributes;
        DELETE FROM products;
        DELETE FROM categories;
        DELETE FROM stored_files
        WHERE purpose = 'product_image'
          AND storage_key LIKE 'products/%';
        """;

    public const string UpsertCategory = """
        INSERT INTO categories (slug, name, sort_order, is_active, is_visible_in_menu)
        VALUES (@Slug, @Name, @SortOrder, @IsActive, @IsVisibleInMenu)
        ON CONFLICT (slug) DO UPDATE
        SET name = EXCLUDED.name,
            sort_order = EXCLUDED.sort_order,
            is_active = EXCLUDED.is_active,
            is_visible_in_menu = EXCLUDED.is_visible_in_menu;
        """;

    public const string UpsertProduct = """
        INSERT INTO products (
            primary_category_id,
            name,
            slug,
            external_id,
            availability_status,
            sale_unit,
            unit_quantity,
            publish_status,
            sort_order)
        SELECT
            category.id,
            @Name,
            @Slug,
            @ExternalId,
            @AvailabilityStatus,
            @SaleUnit,
            @UnitQuantity,
            @PublishStatus,
            @SortOrder
        FROM categories category
        WHERE category.slug = @CategorySlug
        ON CONFLICT (external_id) DO UPDATE
        SET primary_category_id = EXCLUDED.primary_category_id,
            name = EXCLUDED.name,
            slug = EXCLUDED.slug,
            availability_status = EXCLUDED.availability_status,
            sale_unit = EXCLUDED.sale_unit,
            unit_quantity = EXCLUDED.unit_quantity,
            publish_status = EXCLUDED.publish_status,
            sort_order = EXCLUDED.sort_order;
        """;

    public const string UpsertStoredFile = """
        INSERT INTO stored_files (storage_key, original_file_name, content_type, size_bytes, checksum, purpose, status)
        VALUES (@StorageKey, @OriginalFileName, @ContentType, @SizeBytes, @Checksum, 'product_image', 'active')
        ON CONFLICT (storage_key) DO UPDATE
        SET original_file_name = EXCLUDED.original_file_name,
            content_type = EXCLUDED.content_type,
            size_bytes = EXCLUDED.size_bytes,
            checksum = EXCLUDED.checksum,
            purpose = EXCLUDED.purpose,
            status = EXCLUDED.status
        RETURNING id;
        """;

    public const string UpsertProductImage = """
        INSERT INTO product_images (product_id, stored_file_id, alt, title, sort_order, is_main)
        SELECT product.id, @StoredFileId, product.name, product.name, 0, TRUE
        FROM products product
        WHERE product.external_id = @ExternalId
        ON CONFLICT (product_id, stored_file_id) DO UPDATE
        SET alt = EXCLUDED.alt,
            title = EXCLUDED.title,
            sort_order = EXCLUDED.sort_order,
            is_main = EXCLUDED.is_main;
        """;
}

public sealed record CatalogImportApplyOptions(
    bool ResetCatalog,
    bool AllowResetInCurrentEnvironment);

public sealed record CatalogImportApplyResult(
    int CategoriesProcessed,
    int ProductsProcessed,
    int ImagesProcessed);

public sealed class CatalogImportDatabase
{
    private readonly string _connectionString;

    public CatalogImportDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CatalogImportApplyResult> ApplyAsync(
        CatalogImportPlan plan,
        CatalogImportApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (options.ResetCatalog)
        {
            if (!options.AllowResetInCurrentEnvironment)
            {
                throw new InvalidOperationException("Catalog reset is allowed only for explicitly approved dev/QA environments.");
            }

            var protectedReferences = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    CatalogImportDatabaseSql.CountProtectedProductReferences,
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (protectedReferences > 0)
            {
                throw new InvalidOperationException($"Catalog reset refused because {protectedReferences} request item(s) reference products.");
            }

            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.ResetCatalog,
                transaction: transaction,
                cancellationToken: cancellationToken));
        }

        foreach (var category in plan.Categories)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertCategory,
                category,
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (var product in plan.Products)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CatalogImportDatabaseSql.UpsertProduct,
                product,
                transaction,
                cancellationToken: cancellationToken));

            if (product.Image is not null && File.Exists(product.Image.File))
            {
                var file = new FileInfo(product.Image.File);
                var storedFileId = await connection.ExecuteScalarAsync<Guid>(
                    new CommandDefinition(
                        CatalogImportDatabaseSql.UpsertStoredFile,
                        new
                        {
                            StorageKey = product.Image.File.Replace('\\', '/').Replace("Assets/product-images/", "products/"),
                            OriginalFileName = file.Name,
                            ContentType = "image/png",
                            SizeBytes = file.Length,
                            Checksum = product.Image.AssetKey
                        },
                        transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(new CommandDefinition(
                    CatalogImportDatabaseSql.UpsertProductImage,
                    new { product.ExternalId, StoredFileId = storedFileId },
                    transaction,
                    cancellationToken: cancellationToken));
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return new CatalogImportApplyResult(
            plan.Categories.Count,
            plan.Products.Count,
            plan.Products.Count(product => product.Image is not null));
    }
}
```

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter CatalogImportDatabaseSqlTests
dotnet build LineCom.sln -m:1
```

Expected: tests and build pass.

- [ ] **Step 5: Commit**

Run:

```powershell
git add apps/catalog-import.core/Database tests/LineCom.Api.Tests/CatalogImport/CatalogImportDatabaseSqlTests.cs
git commit -m "feat: add catalog import database apply"
```

## Task 5: WinForms Shell

**Files:**

- Create: `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`
- Create: `apps/catalog-import.winforms/Program.cs`
- Create: `apps/catalog-import.winforms/MainForm.cs`
- Modify: `LineCom.sln`

- [ ] **Step 1: Create WinForms project**

Create `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\catalog-import.core\LineCom.CatalogImport.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add WinForms project to solution**

Run:

```powershell
dotnet sln LineCom.sln add apps\catalog-import.winforms\LineCom.CatalogImport.WinForms.csproj
```

Expected: project added.

- [ ] **Step 3: Add WinForms entrypoint**

Create `apps/catalog-import.winforms/Program.cs`:

```csharp
namespace LineCom.CatalogImport.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
```

- [ ] **Step 4: Add main form**

Create `apps/catalog-import.winforms/MainForm.cs`:

```csharp
using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Reporting;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.CatalogImport.WinForms;

public sealed class MainForm : Form
{
    private readonly TextBox _sourcePath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _manifestPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _reportPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _connectionString = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly CheckBox _resetCatalog = new() { Text = "Reset catalog then import (dev/QA only)", AutoSize = true };
    private readonly Button _dryRunButton = new() { Text = "Dry-run", AutoSize = true };
    private readonly Button _writeReportButton = new() { Text = "Write report", AutoSize = true, Enabled = false };
    private readonly DataGridView _productsGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
    private readonly TextBox _log = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

    private CatalogImportPlan? _currentPlan;

    public MainForm()
    {
        Text = "LineCom Catalog Importer";
        Width = 1200;
        Height = 800;

        _sourcePath.Text = Path.Combine("Assets", "1c_export_41_01_nomenclature_by_category.json");
        _manifestPath.Text = Path.Combine("Assets", "product-images", "part1_png_reviewed_manifest.json");
        _reportPath.Text = Path.Combine(".codex-tmp", "catalog-import-reports");

        _dryRunButton.Click += (_, _) => RunDryRun();
        _writeReportButton.Click += (_, _) => WriteReport();

        Controls.Add(BuildLayout());
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        root.Controls.Add(BuildInputs(), 0, 0);
        root.Controls.Add(BuildButtons(), 0, 1);
        root.Controls.Add(_productsGrid, 0, 2);
        root.Controls.Add(_log, 0, 3);

        return root;
    }

    private Control BuildInputs()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddFileRow(panel, "Source JSON", _sourcePath, "JSON files (*.json)|*.json|All files (*.*)|*.*");
        AddFileRow(panel, "Image manifest", _manifestPath, "JSON files (*.json)|*.json|All files (*.*)|*.*");
        AddFolderRow(panel, "Report folder", _reportPath);
        AddTextRow(panel, "Connection string", _connectionString);

        return panel;
    }

    private void AddFileRow(TableLayoutPanel panel, string label, TextBox textBox, string filter)
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
            }
        };
        AddRow(panel, label, textBox, button);
    }

    private void AddFolderRow(TableLayoutPanel panel, string label, TextBox textBox)
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.SelectedPath;
            }
        };
        AddRow(panel, label, textBox, button);
    }

    private void AddTextRow(TableLayoutPanel panel, string label, TextBox textBox)
    {
        AddRow(panel, label, textBox, new Label());
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control input, Control action)
    {
        var row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(input, 1, row);
        panel.Controls.Add(action, 2, row);
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        panel.Controls.Add(_resetCatalog);
        panel.Controls.Add(_dryRunButton);
        panel.Controls.Add(_writeReportButton);
        return panel;
    }

    private void RunDryRun()
    {
        try
        {
            var source = OneCExportReader.Read(_sourcePath.Text);
            var images = ProductImageManifestReader.ReadAcceptedBySourceRow(_manifestPath.Text);
            _currentPlan = CatalogImportPlanner.BuildPlan(source, images);
            _productsGrid.DataSource = _currentPlan.Products.Select(product => new
            {
                product.SourceRow,
                product.ExternalId,
                product.Name,
                product.Slug,
                product.CategorySlug,
                product.PublishStatus,
                HasImage = product.Image is not null
            }).ToArray();
            _writeReportButton.Enabled = true;
            Log($"Dry-run complete. Products: {_currentPlan.Summary.Products}, published: {_currentPlan.Summary.PublishableProducts}, draft: {_currentPlan.Summary.DraftProducts}, images: {_currentPlan.Summary.ImageAssignments}.");
        }
        catch (Exception exception)
        {
            Log(exception.Message);
            MessageBox.Show(this, exception.Message, "Dry-run failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void WriteReport()
    {
        if (_currentPlan is null)
        {
            return;
        }

        var result = CatalogImportReportWriter.WriteReports(
            _currentPlan,
            _reportPath.Text,
            new CatalogImportReportContext(
                _sourcePath.Text,
                _manifestPath.Text,
                _resetCatalog.Checked ? "reset-dry-run" : "dry-run",
                string.IsNullOrWhiteSpace(_connectionString.Text) ? null : "configured"));

        Log($"Reports written: {result.MarkdownPath}");
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTimeOffset.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
```

- [ ] **Step 5: Build solution**

Run:

```powershell
dotnet build LineCom.sln -m:1
```

Expected: build passes. If WinForms targeting requires Windows targeting properties in CI/build context, add the minimal project property required by the SDK and rerun.

- [ ] **Step 6: Commit**

Run:

```powershell
git add apps/catalog-import.winforms LineCom.sln
git commit -m "feat: add catalog import winforms shell"
```

## Task 6: Documentation And Full Verification

**Files:**

- Modify: `vault/Человекочитаемое/Catalog Image Import iterations.md`

- [ ] **Step 1: Update iteration notes**

Append to `vault/Человекочитаемое/Catalog Image Import iterations.md`:

```markdown
## 2026-05-08. WinForms production-like catalog import pipeline

Цель итерации: перейти от тестового seed к production-oriented import pipeline для альфа-каталога.

Решения:

- основной источник: `Assets/1c_export_41_01_nomenclature_by_category.json`;
- UI: WinForms;
- бизнес-логика импорта вынесена в `LineCom.CatalogImport.Core`;
- первый режим: dry-run preview, отчеты и guarded dev/QA apply/reset;
- публичные цены, онлайн-оплата, заказы и публичные остатки не импортируются.

Артефакты:

- spec: `docs/superpowers/specs/2026-05-08-catalog-importer-winforms-design.md`;
- plan: `docs/superpowers/plans/2026-05-08-catalog-importer-winforms.md`;
- core project: `apps/catalog-import.core/LineCom.CatalogImport.Core.csproj`;
- WinForms project: `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.

Проверки:

- `dotnet build LineCom.sln -m:1`;
- `dotnet test LineCom.sln -m:1`;
- `npm.cmd test` from `apps/front`;
- `npm.cmd run build` from `apps/front`.
```

- [ ] **Step 2: Run full verification**

Run:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
```

Then from `apps/front`:

```powershell
npm.cmd test
npm.cmd run build
```

Expected: all pass. Allow existing `NU1900` warnings only if NuGet vulnerability feed is unavailable.

- [ ] **Step 3: Search for forbidden scope and unfinished markers**

Run:

```powershell
rg -n "Купить|В корзину|Розничная цена|Мелкий опт|оплат|TODO|TBD|FIXME|заглуш|костыл" apps/catalog-import.core apps/catalog-import.winforms tests/LineCom.Api.Tests/CatalogImport docs/superpowers/specs docs/superpowers/plans vault/Человекочитаемое
```

Expected:

- no forbidden commerce language in importer implementation;
- documentation matches are only explicit excluded-scope notes or historical notes;
- no unfinished markers in changed implementation files.

- [ ] **Step 4: Commit documentation**

Run:

```powershell
git add vault/Человекочитаемое/Catalog Image Import iterations.md docs/superpowers/plans/2026-05-08-catalog-importer-winforms.md
git commit -m "docs: record catalog importer workflow"
```

## Self-Review

Spec coverage:

- WinForms UI: Task 5.
- UI-independent import core: Tasks 1-4.
- Normalized 1C JSON input: Tasks 1-2.
- Dry-run preview: Tasks 2 and 5.
- Reports: Task 3.
- Guarded reset and apply: Task 4.
- Images from reviewed manifest: Task 2.
- No prices/payment/order automation: Task 4 SQL tests and Task 6 scope search.
- Documentation and verification: Task 6.

No intentional placeholders are left. Attribute normalization, WPF, web admin UI, full 1C synchronization, and legal approval automation remain out of scope by design.
