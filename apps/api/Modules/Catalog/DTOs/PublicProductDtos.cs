namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record PublicProductListResponse(
    IReadOnlyList<PublicProductListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record PublicProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    PublicBrandSummaryDto? Brand,
    PublicCategorySummaryDto Category,
    PublicCodeLabelDto Availability,
    PublicCodeLabelDto SaleUnit,
    string UnitQuantity,
    PublicImageDto? MainImage);

public sealed record PublicProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? Description,
    string? ShortDescription,
    string? H1,
    PublicCategorySummaryDto Category,
    PublicBrandSummaryDto? Brand,
    PublicCodeLabelDto Availability,
    PublicCodeLabelDto SaleUnit,
    string UnitQuantity,
    IReadOnlyList<PublicImageDto> Images,
    IReadOnlyList<PublicProductAttributeDto> Attributes,
    PublicSeoDto Seo,
    IReadOnlyList<PublicBreadcrumbDto> Breadcrumbs);

public sealed record PublicProductAttributeDto(
    string Code,
    string Name,
    string Type,
    string? Unit,
    object Value,
    int SortOrder);
