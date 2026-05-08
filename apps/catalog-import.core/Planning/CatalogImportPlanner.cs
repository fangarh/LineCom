using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.CatalogImport.Core.Planning;

public static class CatalogImportPlanner
{
    private const string DefaultAvailabilityStatus = "check_availability";
    private const string DefaultSaleUnit = "piece";
    private const string DefaultUnitQuantity = "1 шт.";
    private const string DraftPublishStatus = "draft";
    private const string PublishedPublishStatus = "published";
    private const string RequiresReviewWarningCode = "product.requires_review";

    public static CatalogImportPlan BuildPlan(
        OneCExportDocument source,
        IReadOnlyDictionary<int, ProductImageManifestItem>? imagesBySourceRow = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Extraction);
        ArgumentNullException.ThrowIfNull(source.Categories);

        imagesBySourceRow ??= new Dictionary<int, ProductImageManifestItem>();

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
        var usedProductSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 0;

        var sortedItems = source.Categories
            .SelectMany((category, categoryIndex) => (category.Items ?? [])
                .Select((item, itemIndex) => new
                {
                    Category = category,
                    CategoryIndex = categoryIndex,
                    Item = item,
                    ItemIndex = itemIndex
                }))
            .OrderBy(entry => entry.Item.SourceRow)
            .ThenBy(entry => entry.CategoryIndex)
            .ThenBy(entry => entry.ItemIndex);

        foreach (var entry in sortedItems)
        {
            var category = entry.Category;
            var item = entry.Item;
            var publishStatus = DeterminePublishStatus(item);
            if (publishStatus == DraftPublishStatus)
            {
                warnings.Add(new CatalogImportWarning(
                    item.SourceRow,
                    RequiresReviewWarningCode,
                    $"Product at source row {item.SourceRow} requires operator review before publication."));
            }

            products.Add(new CatalogProductImportRow(
                SourceRow: item.SourceRow,
                ExternalId: $"1c:{source.Extraction.SourceAccount}:row:{item.SourceRow}",
                Name: item.Name,
                Slug: SlugGenerator.CreateUniqueSlug(item.Name, usedProductSlugs),
                CategorySlug: category.Slug,
                AvailabilityStatus: DefaultAvailabilityStatus,
                SaleUnit: DefaultSaleUnit,
                UnitQuantity: DefaultUnitQuantity,
                PublishStatus: publishStatus,
                SortOrder: sortOrder,
                Image: CreateImageRow(item.SourceRow, imagesBySourceRow)));

            sortOrder++;
        }

        var summary = new CatalogImportSummary(
            Categories: categories.Length,
            Products: products.Count,
            PublishableProducts: products.Count(product => product.PublishStatus == PublishedPublishStatus),
            DraftProducts: products.Count(product => product.PublishStatus == DraftPublishStatus),
            ImageAssignments: products.Count(product => product.Image is not null),
            Warnings: warnings.Count);

        return new CatalogImportPlan(summary, categories, products, warnings);
    }

    private static string DeterminePublishStatus(OneCExportItem item)
    {
        return string.Equals(item.Classification.Confidence, "high", StringComparison.OrdinalIgnoreCase)
            && !item.Classification.NeedsReview
            ? PublishedPublishStatus
            : DraftPublishStatus;
    }

    private static CatalogProductImageImportRow? CreateImageRow(
        int sourceRow,
        IReadOnlyDictionary<int, ProductImageManifestItem> imagesBySourceRow)
    {
        if (!imagesBySourceRow.TryGetValue(sourceRow, out var image))
        {
            return null;
        }

        return new CatalogProductImageImportRow(
            image.AssetKey,
            image.File,
            string.IsNullOrWhiteSpace(image.RightsStatus) ? ProductImageManifestReader.DefaultRightsStatus : image.RightsStatus);
    }
}
