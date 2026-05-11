namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductListQuery(
    int? Page,
    int? PageSize,
    Guid? CategoryId,
    Guid? BrandId,
    bool? IsActive,
    string? PublishStatus,
    string? Search);

public sealed record AdminProductListResponse(
    IReadOnlyList<AdminProductListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? ExternalId,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    string PublishStatus,
    bool IsActive,
    string AvailabilityStatus,
    int SortOrder,
    AdminProductReadinessDto Readiness);

public sealed record AdminProductDetailDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    string Name,
    string Slug,
    string? Sku,
    string? ExternalId,
    string? Description,
    string? ShortDescription,
    string AvailabilityStatus,
    string SaleUnit,
    string UnitQuantity,
    string PublishStatus,
    bool IsActive,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int SortOrder,
    AdminProductReadinessDto Readiness,
    AdminProductImageSummaryDto Images,
    IReadOnlyList<AdminProductAttributeValueDto> Attributes);

public sealed record AdminProductReadinessDto(
    bool CanPublish,
    IReadOnlyList<AdminProductReadinessIssueDto> Issues);

public sealed record AdminProductReadinessIssueDto(string Code, string Message);

public sealed record AdminProductImageSummaryDto(int ImagesCount, Guid? MainImageFileId);

public sealed record AdminProductAttributeValueDto(
    Guid AttributeId,
    string Code,
    string Name,
    string Type,
    string? Unit,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    Guid? AttributeOptionId,
    string? OptionValue);

public sealed record UpsertAdminProductCommand(
    Guid? CategoryId,
    Guid? BrandId,
    string? Name,
    string? Slug,
    string? Sku,
    string? ExternalId,
    string? Description,
    string? ShortDescription,
    string? AvailabilityStatus,
    string? SaleUnit,
    string? UnitQuantity,
    string? PublishStatus,
    bool? IsActive,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int? SortOrder);
