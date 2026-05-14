namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminProductDapperParameterMapper
{
    public static AdminProductUpsertParameters ToUpsertParameters(AdminProductUpsert command, Guid? id = null)
    {
        return new AdminProductUpsertParameters(
            id,
            command.CategoryId,
            command.BrandId,
            command.Name,
            command.Slug,
            command.Sku,
            command.ExternalId,
            command.Description,
            command.ShortDescription,
            command.AvailabilityStatus,
            command.SaleUnit,
            command.UnitQuantity,
            command.PublishStatus,
            command.IsActive,
            command.SeoTitle,
            command.SeoDescription,
            command.H1,
            command.SortOrder);
    }

    public static AdminProductAttributeValueParameters ToAttributeValueParameters(
        Guid productId,
        AdminProductAttributeValueUpsert command)
    {
        return new AdminProductAttributeValueParameters(
            productId,
            command.AttributeId,
            command.ValueText,
            command.ValueNumber,
            command.ValueBoolean,
            command.AttributeOptionId);
    }
}

internal sealed record AdminProductUpsertParameters(
    Guid? Id,
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

internal sealed record AdminProductAttributeValueParameters(
    Guid ProductId,
    Guid AttributeId,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    Guid? AttributeOptionId);
