namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminProductReadListQuery(
    int Page,
    int PageSize,
    Guid? CategoryId,
    Guid? BrandId,
    bool? IsActive,
    string? PublishStatus,
    string? Search);

public sealed record AdminProductListRecordResponse(
    IReadOnlyList<AdminProductListRecord> Items,
    int TotalItems);

public sealed record AdminProductListRecord(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? ExternalId,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    bool CategoryIsActive,
    Guid? BrandId,
    string? BrandName,
    string PublishStatus,
    bool IsActive,
    string AvailabilityStatus,
    int SortOrder,
    int MissingRequiredAttributeCount,
    int InvalidAttributeValueCount);

public sealed record AdminProductDetailRecord(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    bool CategoryIsActive,
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
    int ImagesCount,
    Guid? MainImageFileId,
    int MissingRequiredAttributeCount,
    int InvalidAttributeValueCount);

public sealed record AdminProductAttributeValueRecord(
    Guid AttributeId,
    string Code,
    string Name,
    string Type,
    string? Unit,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    Guid? AttributeOptionId,
    string? OptionValue,
    bool IsRequired,
    bool IsValidValue);

public sealed record AdminProductUpsert(
    Guid CategoryId,
    Guid? BrandId,
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
    int SortOrder);

public sealed record AdminProductDuplicateIdentity(Guid ProductId, string Field);

public sealed record AdminProductRequiredAttributeRecord(
    Guid AttributeId,
    string Code,
    string Name,
    string Type,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    Guid? AttributeOptionId);

public sealed record AdminProductReadinessMetadata(
    bool CategoryExists,
    bool CategoryIsActive,
    IReadOnlyList<AdminProductRequiredAttributeRecord> RequiredAttributes,
    int InvalidAttributeValueCount);

internal sealed class AdminProductDuplicateIdentityException : Exception
{
    public AdminProductDuplicateIdentityException(string field, Exception? innerException = null)
        : base("Product hard identity already exists.", innerException)
    {
        Field = field;
    }

    public string Field { get; }
}

internal sealed class InvalidAdminProductException : Exception
{
    public InvalidAdminProductException(Exception? innerException = null)
        : base("Product request is invalid.", innerException)
    {
    }
}

internal sealed class AdminProductInUseException : Exception
{
    public AdminProductInUseException(Exception? innerException = null)
        : base("Product is in use.", innerException)
    {
    }
}

public interface IAdminCatalogProductRepository
{
    Task<AdminProductListRecordResponse> GetProductsAsync(
        AdminProductReadListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailRecord?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductAttributeValueRecord>> GetProductAttributesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminProductDuplicateIdentity?> FindDuplicateHardIdentityAsync(
        Guid? excludeProductId,
        string slug,
        string? sku,
        string? externalId,
        CancellationToken cancellationToken = default);

    Task<AdminProductReadinessMetadata> GetReadinessMetadataAsync(
        Guid? productId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailRecord> CreateProductAsync(
        AdminProductUpsert command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailRecord?> UpdateProductAsync(
        Guid id,
        AdminProductUpsert command,
        CancellationToken cancellationToken = default);

    Task<int> CountProductUsageAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
