namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record PublicProductDetailRow(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? Description,
    string? ShortDescription,
    string? H1,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    string? BrandSlug,
    string AvailabilityStatus,
    string SaleUnit,
    string UnitQuantity,
    string? SeoTitle,
    string? SeoDescription);

internal sealed record PublicProductImageRow(
    string Url,
    string Alt,
    string? Title);

internal sealed record PublicProductAttributeRow(
    string Code,
    string Name,
    string Type,
    string? Unit,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    string? OptionValue,
    int SortOrder);

internal sealed record PublicProductCategoryBreadcrumbRow(
    string Name,
    string Slug,
    int Depth);
