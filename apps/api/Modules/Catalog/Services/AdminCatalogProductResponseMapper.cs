using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Modules.Catalog.Services;

internal static class AdminCatalogProductResponseMapper
{
    public static AdminProductListItemDto ToListItemDto(AdminProductListRecord record)
    {
        return new AdminProductListItemDto(
            record.Id,
            record.Name,
            record.Slug,
            record.Sku,
            record.ExternalId,
            record.CategoryName,
            record.CategorySlug,
            record.BrandName,
            record.PublishStatus,
            record.IsActive,
            record.AvailabilityStatus,
            record.SortOrder,
            BuildReadiness(
                record.Name,
                record.Slug,
                record.CategoryId,
                saleUnit: "known",
                unitQuantity: "known",
                record.PublishStatus,
                record.IsActive,
                categoryExists: true,
                record.CategoryIsActive,
                requiredAttributes: [],
                record.InvalidAttributeValueCount,
                record.MissingRequiredAttributeCount));
    }

    public static AdminProductDetailDto ToDetailDto(
        AdminProductDetailRecord product,
        IReadOnlyList<AdminProductAttributeValueRecord> attributes)
    {
        return new AdminProductDetailDto(
            product.Id,
            product.CategoryId,
            product.CategoryName,
            product.BrandId,
            product.BrandName,
            product.Name,
            product.Slug,
            product.Sku,
            product.ExternalId,
            product.Description,
            product.ShortDescription,
            product.AvailabilityStatus,
            product.SaleUnit,
            product.UnitQuantity,
            product.PublishStatus,
            product.IsActive,
            product.SeoTitle,
            product.SeoDescription,
            product.H1,
            product.SortOrder,
            BuildReadiness(
                product.Name,
                product.Slug,
                product.CategoryId,
                product.SaleUnit,
                product.UnitQuantity,
                product.PublishStatus,
                product.IsActive,
                categoryExists: true,
                product.CategoryIsActive,
                attributes
                    .Where(attribute => attribute.IsRequired)
                    .Select(attribute => new AdminProductRequiredAttributeRecord(
                        attribute.AttributeId,
                        attribute.Code,
                        attribute.Name,
                        attribute.Type,
                        attribute.ValueText,
                        attribute.ValueNumber,
                        attribute.ValueBoolean,
                        attribute.AttributeOptionId))
                    .ToArray(),
                product.InvalidAttributeValueCount,
                product.MissingRequiredAttributeCount),
            new AdminProductImageSummaryDto(
                product.ImagesCount,
                product.MainImageFileId),
            attributes.Select(ToAttributeDto).ToArray());
    }

    public static AdminProductReadinessDto BuildReadiness(
        string? name,
        string? slug,
        Guid? categoryId,
        string? saleUnit,
        string? unitQuantity,
        string? publishStatus,
        bool isActive,
        bool categoryExists,
        bool categoryIsActive,
        IReadOnlyList<AdminProductRequiredAttributeRecord> requiredAttributes,
        int invalidAttributeValueCount,
        int missingRequiredAttributeCount = 0)
    {
        var issues = new List<AdminProductReadinessIssueDto>();

        AddIf(issues, !isActive, "product_inactive", "Product is inactive.");
        AddIf(issues, !string.Equals(publishStatus, "published", StringComparison.Ordinal), "product_draft", "Product is not published.");
        AddIf(issues, string.IsNullOrWhiteSpace(name), "missing_name", "Name is required.");
        AddIf(issues, string.IsNullOrWhiteSpace(slug), "missing_slug", "Slug is required.");
        AddIf(issues, categoryId is null || !categoryExists, "missing_category", "Category is required.");
        AddIf(issues, categoryExists && !categoryIsActive, "inactive_category", "Category is inactive.");
        AddIf(issues, string.IsNullOrWhiteSpace(saleUnit), "missing_sale_unit", "Sale unit is required.");
        AddIf(issues, string.IsNullOrWhiteSpace(unitQuantity), "missing_unit_quantity", "Unit quantity is required.");

        var missingRequiredCount = missingRequiredAttributeCount
            + requiredAttributes.Count(attribute => !HasRequiredAttributeValue(attribute));
        AddIf(issues, missingRequiredCount > 0, "missing_required_attribute", "Required attribute value is missing.");
        AddIf(issues, invalidAttributeValueCount > 0, "invalid_attribute_value", "Attribute value type is invalid.");

        return new AdminProductReadinessDto(issues.Count == 0, issues);
    }

    private static AdminProductAttributeValueDto ToAttributeDto(AdminProductAttributeValueRecord record)
    {
        return new AdminProductAttributeValueDto(
            record.AttributeId,
            record.Code,
            record.Name,
            record.Type,
            record.Unit,
            record.ValueText,
            record.ValueNumber,
            record.ValueBoolean,
            record.AttributeOptionId,
            record.OptionValue);
    }

    private static bool HasRequiredAttributeValue(AdminProductRequiredAttributeRecord attribute)
    {
        return attribute.Type switch
        {
            "text" => !string.IsNullOrWhiteSpace(attribute.ValueText),
            "number" => attribute.ValueNumber is not null,
            "boolean" => attribute.ValueBoolean is not null,
            "select" => attribute.AttributeOptionId is not null,
            _ => false
        };
    }

    private static void AddIf(
        List<AdminProductReadinessIssueDto> issues,
        bool condition,
        string code,
        string message)
    {
        if (condition)
        {
            issues.Add(new AdminProductReadinessIssueDto(code, message));
        }
    }
}
