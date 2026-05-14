using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicProductDetailResponseBuilder
{
    public static PublicProductDetailDto Build(
        PublicProductDetailRow? product,
        IReadOnlyCollection<PublicProductImageRow> imageRows,
        IReadOnlyCollection<PublicProductAttributeRow> attributeRows,
        IReadOnlyCollection<PublicProductCategoryBreadcrumbRow> breadcrumbRows,
        IPublicCatalogReferenceData referenceData)
    {
        if (product is null)
        {
            throw PublicCatalogErrors.ProductNotFound();
        }

        var breadcrumbs = breadcrumbRows
            .OrderByDescending(row => row.Depth)
            .Select(row => new PublicBreadcrumbDto(row.Name, row.Slug))
            .Append(new PublicBreadcrumbDto(product.Name, product.Slug))
            .ToArray();

        return new PublicProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Sku,
            product.Description,
            product.ShortDescription,
            product.H1,
            new PublicCategorySummaryDto(product.CategoryName, product.CategorySlug),
            BuildBrand(product),
            referenceData.GetAvailability(product.AvailabilityStatus),
            referenceData.GetSaleUnit(product.SaleUnit),
            product.UnitQuantity,
            imageRows
                .Select(row => new PublicImageDto(row.Url, row.Alt, row.Title))
                .ToArray(),
            attributeRows
                .Select(BuildAttribute)
                .ToArray(),
            new PublicSeoDto(
                product.SeoTitle,
                product.SeoDescription,
                $"/products/{product.Slug}"),
            breadcrumbs);
    }

    private static PublicBrandSummaryDto? BuildBrand(PublicProductDetailRow product)
    {
        return product.BrandName is not null && product.BrandSlug is not null
            ? new PublicBrandSummaryDto(product.BrandName, product.BrandSlug)
            : null;
    }

    private static PublicProductAttributeDto BuildAttribute(PublicProductAttributeRow row)
    {
        return new PublicProductAttributeDto(
            row.Code,
            row.Name,
            row.Type,
            row.Unit,
            ResolveValue(row),
            row.SortOrder);
    }

    private static object ResolveValue(PublicProductAttributeRow row)
    {
        return row.Type switch
        {
            "text" => row.ValueText ?? throw InvalidAttributeValue(row),
            "number" => row.ValueNumber ?? throw InvalidAttributeValue(row),
            "boolean" => row.ValueBoolean ?? throw InvalidAttributeValue(row),
            "select" => row.OptionValue ?? throw InvalidAttributeValue(row),
            _ => throw InvalidAttributeValue(row)
        };
    }

    private static InvalidOperationException InvalidAttributeValue(PublicProductAttributeRow row)
    {
        return new InvalidOperationException($"Invalid public product attribute value for '{row.Code}'.");
    }
}
