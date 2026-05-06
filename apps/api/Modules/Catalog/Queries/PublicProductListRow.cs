namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record PublicProductListRow(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? BrandName,
    string? BrandSlug,
    string CategoryName,
    string CategorySlug,
    string AvailabilityStatus,
    string SaleUnit,
    string UnitQuantity,
    string? MainImageUrl,
    string? MainImageAlt,
    string? MainImageTitle);
