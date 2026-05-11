using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminCatalogAttributeService : IAdminCatalogAttributeService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "text",
        "number",
        "select",
        "boolean"
    };

    private const string AttributeInUseMessage = "\u0425\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0438\u0441\u0442\u0438\u043a\u0430 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0442\u043e\u0432\u0430\u0440\u0430\u043c\u0438.";
    private const string OptionInUseMessage = "\u041e\u043f\u0446\u0438\u044f \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0442\u043e\u0432\u0430\u0440\u0430\u043c\u0438.";

    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminCatalogAttributeRepository _repository;

    public AdminCatalogAttributeService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminCatalogAttributeRepository repository)
    {
        _staffGuard = staffGuard;
        _repository = repository;
    }

    public async Task<AdminCategoryAttributesResponse> GetAttributesAsync(
        HttpContext httpContext,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var attributes = await _repository.GetAttributesAsync(categoryId, cancellationToken);
        var options = await _repository.GetOptionsAsync(categoryId, cancellationToken);
        var optionsByAttribute = options.ToLookup(option => option.AttributeId);

        return new AdminCategoryAttributesResponse(attributes
            .Select(attribute => ToDto(attribute, optionsByAttribute[attribute.Id]))
            .ToArray());
    }

    public async Task<AdminCategoryAttributeDto> CreateAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            var record = await _repository.CreateAttributeAsync(
                categoryId,
                ToAttributeUpsert(command),
                cancellationToken);

            return ToDto(record, []);
        }
        catch (AdminCatalogAttributeDuplicateException)
        {
            throw Duplicate();
        }
        catch (InvalidAdminCatalogAttributeException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminCategoryAttributeDto> UpdateAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var existing = await _repository.GetAttributeAsync(categoryId, attributeId, cancellationToken);
        if (existing is null)
        {
            throw AttributeNotFound();
        }

        var upsert = ToAttributeUpsert(command);
        if (!string.Equals(existing.Type, upsert.Type, StringComparison.Ordinal)
            && existing.ProductValuesCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(AttributeInUseMessage);
        }

        if (string.Equals(existing.Type, "select", StringComparison.Ordinal)
            && !string.Equals(upsert.Type, "select", StringComparison.Ordinal))
        {
            var options = await _repository.GetOptionsAsync(categoryId, cancellationToken);
            if (options.Any(option => option.AttributeId == attributeId))
            {
                throw AdminCatalogErrors.InvalidRequest();
            }
        }

        AdminCategoryAttributeRecord? record;
        try
        {
            record = await _repository.UpdateAttributeAsync(
                categoryId,
                attributeId,
                upsert,
                cancellationToken);
        }
        catch (AdminCatalogAttributeDuplicateException)
        {
            throw Duplicate();
        }
        catch (InvalidAdminCatalogAttributeException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        if (record is null)
        {
            throw AttributeNotFound();
        }

        return ToDto(record, []);
    }

    public async Task DeleteAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var existing = await _repository.GetAttributeAsync(categoryId, attributeId, cancellationToken);
        if (existing is null)
        {
            throw AttributeNotFound();
        }

        if (existing.ProductValuesCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(AttributeInUseMessage);
        }

        bool deleted;
        try
        {
            deleted = await _repository.DeleteAttributeAsync(categoryId, attributeId, cancellationToken);
        }
        catch (AdminCatalogAttributeInUseException)
        {
            throw AdminCatalogErrors.EntityInUse(AttributeInUseMessage);
        }

        if (!deleted)
        {
            var latest = await _repository.GetAttributeAsync(categoryId, attributeId, cancellationToken);
            if (latest?.ProductValuesCount > 0)
            {
                throw AdminCatalogErrors.EntityInUse(AttributeInUseMessage);
            }

            throw AttributeNotFound();
        }
    }

    public async Task<AdminAttributeOptionDto> CreateOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);
        await RequireSelectAttributeAsync(categoryId, attributeId, cancellationToken);

        try
        {
            var record = await _repository.CreateOptionAsync(
                categoryId,
                attributeId,
                ToOptionUpsert(command),
                cancellationToken);

            return ToDto(record);
        }
        catch (AdminCatalogAttributeDuplicateException)
        {
            throw Duplicate();
        }
        catch (InvalidAdminCatalogAttributeException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminAttributeOptionDto> UpdateOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);
        await RequireSelectAttributeAsync(categoryId, attributeId, cancellationToken);

        AdminAttributeOptionRecord? record;
        try
        {
            record = await _repository.UpdateOptionAsync(
                categoryId,
                attributeId,
                optionId,
                ToOptionUpsert(command),
                cancellationToken);
        }
        catch (AdminCatalogAttributeDuplicateException)
        {
            throw Duplicate();
        }
        catch (InvalidAdminCatalogAttributeException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        if (record is null)
        {
            throw OptionNotFound();
        }

        return ToDto(record);
    }

    public async Task DeleteOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var option = await _repository.GetOptionAsync(categoryId, attributeId, optionId, cancellationToken);
        if (option is null)
        {
            throw OptionNotFound();
        }

        if (option.ProductValuesCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(OptionInUseMessage);
        }

        bool deleted;
        try
        {
            deleted = await _repository.DeleteOptionAsync(categoryId, attributeId, optionId, cancellationToken);
        }
        catch (AdminCatalogAttributeInUseException)
        {
            throw AdminCatalogErrors.EntityInUse(OptionInUseMessage);
        }

        if (!deleted)
        {
            var latest = await _repository.GetOptionAsync(categoryId, attributeId, optionId, cancellationToken);
            if (latest?.ProductValuesCount > 0)
            {
                throw AdminCatalogErrors.EntityInUse(OptionInUseMessage);
            }

            throw OptionNotFound();
        }
    }

    public async Task<InheritAdminCategoryAttributesResponse> InheritFromParentAsync(
        HttpContext httpContext,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var result = await _repository.InheritFromParentAsync(categoryId, cancellationToken);

        return new InheritAdminCategoryAttributesResponse(result.Added, result.Skipped);
    }

    private async Task RequireSelectAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        var attribute = await _repository.GetAttributeAsync(categoryId, attributeId, cancellationToken);
        if (attribute is null)
        {
            throw AttributeNotFound();
        }

        if (!string.Equals(attribute.Type, "select", StringComparison.Ordinal))
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    private static AdminCategoryAttributeUpsert ToAttributeUpsert(UpsertAdminCategoryAttributeCommand command)
    {
        var type = AdminCatalogInput.RequireText(command.Type);
        if (!AllowedTypes.Contains(type))
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        return new AdminCategoryAttributeUpsert(
            AdminCatalogInput.RequireText(command.Name),
            AdminCatalogInput.RequireText(command.Code),
            type,
            AdminCatalogInput.NormalizeText(command.Unit),
            command.IsRequired ?? false,
            command.IsFilterable ?? false,
            command.IsComparable ?? false,
            command.IsVisibleInProduct ?? true,
            command.IsSeoImportant ?? false,
            command.IsUsedInGeneratedName ?? false,
            command.SortOrder ?? 0,
            command.IsActive ?? true);
    }

    private static AdminAttributeOptionUpsert ToOptionUpsert(UpsertAdminAttributeOptionCommand command)
    {
        return new AdminAttributeOptionUpsert(
            AdminCatalogInput.RequireText(command.Value),
            AdminCatalogInput.RequireText(command.Slug),
            AdminCatalogInput.RequireText(command.NormalizedValue),
            command.SortOrder ?? 0,
            command.IsActive ?? true);
    }

    private static AdminCategoryAttributeDto ToDto(
        AdminCategoryAttributeRecord record,
        IEnumerable<AdminAttributeOptionRecord> options)
    {
        return new AdminCategoryAttributeDto(
            record.Id,
            record.CategoryId,
            record.Name,
            record.Code,
            record.Type,
            record.Unit,
            record.IsRequired,
            record.IsFilterable,
            record.IsComparable,
            record.IsVisibleInProduct,
            record.IsSeoImportant,
            record.IsUsedInGeneratedName,
            record.SortOrder,
            record.IsActive,
            record.ProductValuesCount,
            options.Select(ToDto).ToArray());
    }

    private static AdminAttributeOptionDto ToDto(AdminAttributeOptionRecord record)
    {
        return new AdminAttributeOptionDto(
            record.Id,
            record.Value,
            record.Slug,
            record.NormalizedValue,
            record.SortOrder,
            record.IsActive,
            record.ProductValuesCount);
    }

    private static ApiException AttributeNotFound()
    {
        return new ApiException(
            "admin_catalog.attribute_not_found",
            "\u0425\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0438\u0441\u0442\u0438\u043a\u0430 \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d\u0430.",
            StatusCodes.Status404NotFound);
    }

    private static ApiException OptionNotFound()
    {
        return new ApiException(
            "admin_catalog.attribute_option_not_found",
            "\u041e\u043f\u0446\u0438\u044f \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d\u0430.",
            StatusCodes.Status404NotFound);
    }

    private static ApiException Duplicate()
    {
        return new ApiException(
            "admin_catalog.duplicate_attribute_value",
            "\u0417\u043d\u0430\u0447\u0435\u043d\u0438\u0435 \u0443\u0436\u0435 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f.",
            StatusCodes.Status409Conflict);
    }
}
