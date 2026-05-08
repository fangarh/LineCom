using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class CatalogImportPlannerTests
{
    [Fact]
    public void BuildPlan_CreatesDeterministicDryRunRows()
    {
        var source = CreateSource();
        var imagesBySourceRow = new Dictionary<int, ProductImageManifestItem>
        {
            [10] = new("accepted", "Assets/product-images/accepted.png", "requires-permission")
        };

        var plan = CatalogImportPlanner.BuildPlan(source, imagesBySourceRow);

        Assert.Equal(2, plan.Categories.Count);
        Assert.Equal("twisted-pair-cable", plan.Categories[0].Slug);
        Assert.Equal("Витая пара", plan.Categories[0].Name);
        Assert.Equal(0, plan.Categories[0].SortOrder);
        Assert.True(plan.Categories[0].IsActive);
        Assert.True(plan.Categories[0].IsVisibleInMenu);
        Assert.Equal("connectors", plan.Categories[1].Slug);
        Assert.Equal(1, plan.Categories[1].SortOrder);

        Assert.Equal(3, plan.Products.Count);
        Assert.Equal([10, 11, 12], plan.Products.Select(product => product.SourceRow).ToArray());
        Assert.Equal([0, 1, 2], plan.Products.Select(product => product.SortOrder).ToArray());

        var first = plan.Products[0];
        Assert.Equal("1c:41.01:row:10", first.ExternalId);
        Assert.Equal("Кабель LANMAX UTP4 cat.5e, 305m, Cu", first.Name);
        Assert.Equal("kabel-lanmax-utp4-cat-5e-305m-cu", first.Slug);
        Assert.Equal("twisted-pair-cable", first.CategorySlug);
        Assert.Equal("check_availability", first.AvailabilityStatus);
        Assert.Equal("piece", first.SaleUnit);
        Assert.Equal("1 шт.", first.UnitQuantity);
        Assert.Equal("published", first.PublishStatus);
        Assert.NotNull(first.Image);
        Assert.Equal("accepted", first.Image.AssetKey);
        Assert.Equal("Assets/product-images/accepted.png", first.Image.File);
        Assert.Equal("requires-permission", first.Image.RightsStatus);

        Assert.Equal("draft", plan.Products[1].PublishStatus);
        Assert.Null(plan.Products[1].Image);
        Assert.Equal("draft", plan.Products[2].PublishStatus);

        Assert.Equal(2, plan.Warnings.Count);
        Assert.All(plan.Warnings, warning => Assert.Equal("product.requires_review", warning.Code));
        Assert.Contains(plan.Warnings, warning => warning.SourceRow == 11);
        Assert.Contains(plan.Warnings, warning => warning.SourceRow == 12);
    }

    [Fact]
    public void BuildPlan_SummaryCountsProductsPublicationAndImages()
    {
        var source = CreateSource();
        var imagesBySourceRow = new Dictionary<int, ProductImageManifestItem>
        {
            [10] = new("accepted", "Assets/product-images/accepted.png", "requires-permission")
        };

        var plan = CatalogImportPlanner.BuildPlan(source, imagesBySourceRow);

        Assert.Equal(2, plan.Summary.Categories);
        Assert.Equal(3, plan.Summary.Products);
        Assert.Equal(1, plan.Summary.PublishableProducts);
        Assert.Equal(2, plan.Summary.DraftProducts);
        Assert.Equal(1, plan.Summary.ImageAssignments);
        Assert.Equal(2, plan.Summary.Warnings);
    }

    [Fact]
    public void BuildPlan_SortsProductsBySourceRowBeforeAssigningSortOrder()
    {
        var source = new OneCExportDocument(
            Source: new OneCExportSource("source.xlsx", "Sheet1", "41.01"),
            Extraction: new OneCExportExtraction("41.01", null, 2, null, null),
            Categories:
            [
                new OneCExportCategory(
                    Slug: "first-category",
                    Name: "First category",
                    ProjectCoreCategory: true,
                    ItemCount: 1,
                    Items:
                    [
                        new OneCExportItem(
                            SourceRow: 20,
                            Name: "First category item",
                            SourceAccount: "41.01",
                            Quantity: 1,
                            UnitCost: 10,
                            Amount: 10,
                            Classification: new OneCExportClassification(
                                CategorySlug: "first-category",
                                CategoryName: "First category",
                                Confidence: "high",
                                MatchedKeywords: ["first"],
                                NeedsReview: false))
                    ]),
                new OneCExportCategory(
                    Slug: "second-category",
                    Name: "Second category",
                    ProjectCoreCategory: true,
                    ItemCount: 1,
                    Items:
                    [
                        new OneCExportItem(
                            SourceRow: 10,
                            Name: "Second category item",
                            SourceAccount: "41.01",
                            Quantity: 1,
                            UnitCost: 10,
                            Amount: 10,
                            Classification: new OneCExportClassification(
                                CategorySlug: "second-category",
                                CategoryName: "Second category",
                                Confidence: "high",
                                MatchedKeywords: ["second"],
                                NeedsReview: false))
                    ])
            ]);

        var plan = CatalogImportPlanner.BuildPlan(source);

        Assert.Equal([10, 20], plan.Products.Select(product => product.SourceRow).ToArray());
        Assert.Equal([0, 1], plan.Products.Select(product => product.SortOrder).ToArray());
    }

    private static OneCExportDocument CreateSource()
    {
        return new OneCExportDocument(
            Source: new OneCExportSource("source.xlsx", "Sheet1", "ОСВ 41.01"),
            Extraction: new OneCExportExtraction("41.01", null, 3, null, null),
            Categories:
            [
                new OneCExportCategory(
                    Slug: "twisted-pair-cable",
                    Name: "Витая пара",
                    ProjectCoreCategory: true,
                    ItemCount: 2,
                    Items:
                    [
                        new OneCExportItem(
                            SourceRow: 10,
                            Name: "Кабель LANMAX UTP4 cat.5e, 305m, Cu",
                            SourceAccount: "41.01",
                            Quantity: 100,
                            UnitCost: 20,
                            Amount: 2000,
                            Classification: new OneCExportClassification(
                                CategorySlug: "twisted-pair-cable",
                                CategoryName: "Витая пара",
                                Confidence: "high",
                                MatchedKeywords: ["utp"],
                                NeedsReview: false)),
                        new OneCExportItem(
                            SourceRow: 11,
                            Name: "Кабель LANMAX UTP4 cat.5e, 305m, Cu",
                            SourceAccount: "41.01",
                            Quantity: 1,
                            UnitCost: 10,
                            Amount: 10,
                            Classification: new OneCExportClassification(
                                CategorySlug: "twisted-pair-cable",
                                CategoryName: "Витая пара",
                                Confidence: "high",
                                MatchedKeywords: ["utp"],
                                NeedsReview: true))
                    ]),
                new OneCExportCategory(
                    Slug: "connectors",
                    Name: "Разъемы",
                    ProjectCoreCategory: true,
                    ItemCount: 1,
                    Items:
                    [
                        new OneCExportItem(
                            SourceRow: 12,
                            Name: "Разъем RJ-45 8P8C",
                            SourceAccount: "41.01",
                            Quantity: 50,
                            UnitCost: 5,
                            Amount: 250,
                            Classification: new OneCExportClassification(
                                CategorySlug: "connectors",
                                CategoryName: "Разъемы",
                                Confidence: "medium",
                                MatchedKeywords: ["rj-45"],
                                NeedsReview: false))
                    ])
            ]);
    }
}
