namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record AdminHomepageSectionRow(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit,
    int SortOrder,
    bool IsActive);

internal sealed record AdminHomepageSectionItemRow(
    Guid Id,
    Guid SectionId,
    Guid? ProductId,
    Guid? CategoryId,
    string? ProductName,
    string? ProductSlug,
    string? ProductSku,
    bool? ProductIsActive,
    string? ProductPublishStatus,
    string? ProductCategoryName,
    bool? ProductCategoryIsActive,
    string? CategoryName,
    string? CategorySlug,
    bool? CategoryIsActive,
    int SortOrder,
    bool IsActive);
