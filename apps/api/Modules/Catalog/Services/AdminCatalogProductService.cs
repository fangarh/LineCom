using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminCatalogProductService : IAdminCatalogProductService
{
    private const string ProductInUseMessage = "\u0422\u043e\u0432\u0430\u0440 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0432 \u0437\u0430\u044f\u0432\u043a\u0430\u0445 \u0438\u043b\u0438 \u043d\u0430 \u0433\u043b\u0430\u0432\u043d\u043e\u0439.";

    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminCatalogProductRepository _repository;
    private readonly IAdminProductDuplicateQuery _duplicateQuery;

    public AdminCatalogProductService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminCatalogProductRepository repository,
        IAdminProductDuplicateQuery duplicateQuery)
    {
        _staffGuard = staffGuard;
        _repository = repository;
        _duplicateQuery = duplicateQuery;
    }

    public async Task<AdminProductListResponse> GetProductsAsync(
        HttpContext httpContext,
        AdminProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var page = AdminCatalogInput.NormalizePage(query.Page);
        var pageSize = AdminCatalogInput.NormalizePageSize(query.PageSize);
        var result = await _repository.GetProductsAsync(
            new AdminProductReadListQuery(
                page,
                pageSize,
                query.CategoryId,
                query.BrandId,
                query.IsActive,
                AdminCatalogInput.NormalizeText(query.PublishStatus),
                AdminCatalogInput.NormalizeText(query.Search)),
            cancellationToken);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)pageSize);

        return new AdminProductListResponse(
            result.Items.Select(ToListItemDto).ToArray(),
            page,
            pageSize,
            result.TotalItems,
            totalPages);
    }

    public async Task<AdminProductDuplicateCandidatesResponse> FindDuplicateCandidatesAsync(
        HttpContext httpContext,
        AdminProductDuplicateCandidatesQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var name = AdminCatalogInput.NormalizeText(query.Name);
        var sku = AdminCatalogInput.NormalizeText(query.Sku);
        var externalId = AdminCatalogInput.NormalizeText(query.ExternalId);
        var slug = AdminCatalogInput.NormalizeText(query.Slug);

        if (name is null && sku is null && externalId is null && slug is null)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        return await _duplicateQuery.FindCandidatesAsync(
            new AdminProductDuplicateCandidateQuery(
                name,
                query.CategoryId,
                query.BrandId,
                sku,
                externalId,
                slug,
                query.ExcludeProductId,
                Math.Clamp(query.Limit ?? 10, 1, 25),
                Math.Clamp(query.SimilarityThreshold ?? 0.35m, 0m, 1m)),
            cancellationToken);
    }

    public async Task<AdminProductDetailDto> GetProductAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var product = await _repository.GetProductAsync(id, cancellationToken);
        if (product is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var attributes = await _repository.GetProductAttributesAsync(id, cancellationToken);

        return ToDetailDto(product, attributes);
    }

    public async Task<AdminProductDetailDto> CreateProductAsync(
        HttpContext httpContext,
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var upsert = ToUpsert(command);
        await ThrowIfDuplicateHardIdentityAsync(null, upsert, cancellationToken);
        await ThrowIfPublishingNotReadyAsync(null, upsert, cancellationToken);

        try
        {
            var product = await _repository.CreateProductAsync(upsert, cancellationToken);
            var attributes = await _repository.GetProductAttributesAsync(product.Id, cancellationToken);

            return ToDetailDto(product, attributes);
        }
        catch (AdminProductDuplicateIdentityException exception)
        {
            throw DuplicateIdentity(exception.Field);
        }
        catch (InvalidAdminProductException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminProductDetailDto> UpdateProductAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var upsert = ToUpsert(command);
        var existingProduct = await _repository.GetProductAsync(id, cancellationToken);
        if (existingProduct is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        await ThrowIfDuplicateHardIdentityAsync(id, upsert, cancellationToken);
        await ThrowIfPublishingNotReadyAsync(id, upsert, cancellationToken);

        AdminProductDetailRecord? product;
        try
        {
            product = await _repository.UpdateProductAsync(id, upsert, cancellationToken);
        }
        catch (AdminProductDuplicateIdentityException exception)
        {
            throw DuplicateIdentity(exception.Field);
        }
        catch (InvalidAdminProductException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
        if (product is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var attributes = await _repository.GetProductAttributesAsync(id, cancellationToken);

        return ToDetailDto(product, attributes);
    }

    public async Task<AdminProductDetailDto> UpdateAttributesAsync(
        HttpContext httpContext,
        Guid id,
        UpdateAdminProductAttributesCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        AdminProductDetailRecord? product;
        try
        {
            product = await _repository.UpdateProductAttributesAsync(
                id,
                ToAttributeValueUpserts(command),
                cancellationToken);
        }
        catch (InvalidAdminProductException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
        catch (AdminProductNotReadyException)
        {
            throw AdminCatalogErrors.ProductNotReady();
        }

        if (product is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var attributes = await _repository.GetProductAttributesAsync(id, cancellationToken);

        return ToDetailDto(product, attributes);
    }

    public async Task DeleteProductAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var product = await _repository.GetProductAsync(id, cancellationToken);
        if (product is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var usageCount = await _repository.CountProductUsageAsync(id, cancellationToken);
        if (usageCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(ProductInUseMessage);
        }

        bool deleted;
        try
        {
            deleted = await _repository.DeleteProductAsync(id, cancellationToken);
        }
        catch (AdminProductInUseException)
        {
            throw AdminCatalogErrors.EntityInUse(ProductInUseMessage);
        }

        if (!deleted)
        {
            var latestUsageCount = await _repository.CountProductUsageAsync(id, cancellationToken);
            if (latestUsageCount > 0)
            {
                throw AdminCatalogErrors.EntityInUse(ProductInUseMessage);
            }

            throw AdminCatalogErrors.ProductNotFound();
        }
    }

    private async Task ThrowIfDuplicateHardIdentityAsync(
        Guid? excludeProductId,
        AdminProductUpsert upsert,
        CancellationToken cancellationToken)
    {
        var duplicate = await _repository.FindDuplicateHardIdentityAsync(
            excludeProductId,
            upsert.Slug,
            upsert.Sku,
            upsert.ExternalId,
            cancellationToken);
        if (duplicate is not null)
        {
            throw DuplicateIdentity(duplicate.Field);
        }
    }

    private async Task ThrowIfPublishingNotReadyAsync(
        Guid? productId,
        AdminProductUpsert upsert,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(upsert.PublishStatus, "published", StringComparison.Ordinal))
        {
            return;
        }

        var metadata = await _repository.GetReadinessMetadataAsync(
            productId,
            upsert.CategoryId,
            cancellationToken);
        var readiness = BuildReadiness(
            upsert.Name,
            upsert.Slug,
            upsert.CategoryId,
            upsert.SaleUnit,
            upsert.UnitQuantity,
            upsert.PublishStatus,
            upsert.IsActive,
            metadata.CategoryExists,
            metadata.CategoryIsActive,
            metadata.RequiredAttributes,
            metadata.InvalidAttributeValueCount);

        if (!readiness.CanPublish)
        {
            throw AdminCatalogErrors.ProductNotReady();
        }
    }

    private static AdminProductUpsert ToUpsert(UpsertAdminProductCommand command)
    {
        var categoryId = command.CategoryId ?? throw AdminCatalogErrors.InvalidRequest();

        return new AdminProductUpsert(
            categoryId,
            command.BrandId,
            AdminCatalogInput.RequireText(command.Name),
            AdminCatalogInput.RequireText(command.Slug),
            AdminCatalogInput.NormalizeText(command.Sku),
            AdminCatalogInput.NormalizeText(command.ExternalId),
            AdminCatalogInput.NormalizeText(command.Description),
            AdminCatalogInput.NormalizeText(command.ShortDescription),
            AdminCatalogInput.RequireText(command.AvailabilityStatus),
            AdminCatalogInput.RequireText(command.SaleUnit),
            AdminCatalogInput.RequireText(command.UnitQuantity),
            AdminCatalogInput.RequireText(command.PublishStatus),
            command.IsActive ?? true,
            AdminCatalogInput.NormalizeText(command.SeoTitle),
            AdminCatalogInput.NormalizeText(command.SeoDescription),
            AdminCatalogInput.NormalizeText(command.H1),
            command.SortOrder ?? 0);
    }

    private static IReadOnlyList<AdminProductAttributeValueUpsert> ToAttributeValueUpserts(
        UpdateAdminProductAttributesCommand command)
    {
        var values = command.Values ?? throw AdminCatalogErrors.InvalidRequest();
        var attributeIds = new HashSet<Guid>();
        var result = new List<AdminProductAttributeValueUpsert>(values.Count);

        foreach (var value in values)
        {
            if (value.AttributeId == Guid.Empty || !attributeIds.Add(value.AttributeId))
            {
                throw AdminCatalogErrors.InvalidRequest();
            }

            var valueText = AdminCatalogInput.NormalizeText(value.ValueText);
            var storageColumnCount = CountPresent(valueText, value.ValueNumber, value.ValueBoolean, value.AttributeOptionId);
            if (storageColumnCount != 1)
            {
                throw AdminCatalogErrors.InvalidRequest();
            }

            result.Add(new AdminProductAttributeValueUpsert(
                value.AttributeId,
                valueText,
                value.ValueNumber,
                value.ValueBoolean,
                value.AttributeOptionId));
        }

        return result;
    }

    private static int CountPresent(
        string? valueText,
        decimal? valueNumber,
        bool? valueBoolean,
        Guid? attributeOptionId)
    {
        var count = 0;
        if (valueText is not null) count++;
        if (valueNumber is not null) count++;
        if (valueBoolean is not null) count++;
        if (attributeOptionId is not null) count++;
        return count;
    }

    private static AdminProductListItemDto ToListItemDto(AdminProductListRecord record)
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

    private static AdminProductDetailDto ToDetailDto(
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

    private static AdminProductReadinessDto BuildReadiness(
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

    private static Exception DuplicateIdentity(string field)
    {
        return field switch
        {
            "sku" => AdminCatalogErrors.SkuAlreadyExists(),
            "external_id" => AdminCatalogErrors.ExternalIdAlreadyExists(),
            _ => AdminCatalogErrors.SlugAlreadyExists()
        };
    }
}
