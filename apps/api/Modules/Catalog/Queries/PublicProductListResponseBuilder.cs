using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicProductListResponseBuilder
{
    public static PublicProductListResponse Build(
        IReadOnlyCollection<PublicProductListRow> rows,
        int page,
        int pageSize,
        int totalItems,
        IPublicCatalogReferenceData referenceData)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Select(row => new PublicProductListItemDto(
                row.Id,
                row.Name,
                row.Slug,
                row.Sku,
                BuildBrand(row),
                new PublicCategorySummaryDto(row.CategoryName, row.CategorySlug),
                referenceData.GetAvailability(row.AvailabilityStatus),
                referenceData.GetSaleUnit(row.SaleUnit),
                row.UnitQuantity,
                BuildMainImage(row)))
            .ToArray();

        return new PublicProductListResponse(items, page, pageSize, totalItems, totalPages);
    }

    private static PublicBrandSummaryDto? BuildBrand(PublicProductListRow row)
    {
        return row.BrandName is not null && row.BrandSlug is not null
            ? new PublicBrandSummaryDto(row.BrandName, row.BrandSlug)
            : null;
    }

    private static PublicImageDto? BuildMainImage(PublicProductListRow row)
    {
        return row.MainImageUrl is not null && row.MainImageAlt is not null
            ? new PublicImageDto(row.MainImageUrl, row.MainImageAlt, row.MainImageTitle)
            : null;
    }
}
