using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminCatalogCategoryService : IAdminCatalogCategoryService
{
    private const string CategoryInUseMessage = "\u041a\u0430\u0442\u0435\u0433\u043e\u0440\u0438\u044f \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0438 \u043d\u0435 \u043c\u043e\u0436\u0435\u0442 \u0431\u044b\u0442\u044c \u0443\u0434\u0430\u043b\u0435\u043d\u0430.";

    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminCatalogCategoryRepository _repository;

    public AdminCatalogCategoryService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminCatalogCategoryRepository repository)
    {
        _staffGuard = staffGuard;
        _repository = repository;
    }

    public async Task<AdminCategoryListResponse> GetCategoriesAsync(
        HttpContext httpContext,
        AdminCategoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var page = AdminCatalogInput.NormalizePage(query.Page);
        var pageSize = AdminCatalogInput.NormalizePageSize(query.PageSize);
        var result = await _repository.GetCategoriesAsync(
            new AdminCategoryReadListQuery(
                page,
                pageSize,
                query.ParentId,
                AdminCatalogInput.NormalizeText(query.Search),
                query.IsActive),
            cancellationToken);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)pageSize);

        return new AdminCategoryListResponse(
            result.Items.Select(ToListItemDto).ToArray(),
            page,
            pageSize,
            result.TotalItems,
            totalPages);
    }

    public async Task<AdminCategoryDetailDto> GetCategoryAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var record = await _repository.GetCategoryAsync(id, cancellationToken);
        if (record is null)
        {
            throw AdminCatalogErrors.CategoryNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminCategoryDetailDto> CreateCategoryAsync(
        HttpContext httpContext,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            var record = await _repository.CreateCategoryAsync(ToUpsert(command), cancellationToken);

            return ToDetailDto(record);
        }
        catch (AdminCategorySlugAlreadyExistsException)
        {
            throw AdminCatalogErrors.SlugAlreadyExists();
        }
        catch (InvalidAdminCategoryParentException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminCategoryDetailDto> UpdateCategoryAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        AdminCategoryRecord? record;
        try
        {
            record = await _repository.UpdateCategoryAsync(id, ToUpsert(command), cancellationToken);
        }
        catch (AdminCategorySlugAlreadyExistsException)
        {
            throw AdminCatalogErrors.SlugAlreadyExists();
        }
        catch (InvalidAdminCategoryParentException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        if (record is null)
        {
            throw AdminCatalogErrors.CategoryNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminCategoryDetailDto> MoveCategoryAsync(
        HttpContext httpContext,
        Guid id,
        MoveAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        AdminCategoryRecord? record;
        try
        {
            record = await _repository.MoveCategoryAsync(id, command.ParentId, cancellationToken);
        }
        catch (InvalidAdminCategoryParentException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        if (record is null)
        {
            throw AdminCatalogErrors.CategoryNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminCategoryDetailDto> SortCategoryAsync(
        HttpContext httpContext,
        Guid id,
        SortAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var record = await _repository.SortCategoryAsync(id, command.SortOrder, cancellationToken);
        if (record is null)
        {
            throw AdminCatalogErrors.CategoryNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task DeleteCategoryAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var usageCount = await _repository.CountCategoryUsageAsync(id, cancellationToken);
        if (usageCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(CategoryInUseMessage);
        }

        bool deleted;
        try
        {
            deleted = await _repository.DeleteCategoryAsync(id, cancellationToken);
        }
        catch (AdminCategoryInUseException)
        {
            throw AdminCatalogErrors.EntityInUse(CategoryInUseMessage);
        }

        if (!deleted)
        {
            throw AdminCatalogErrors.CategoryNotFound();
        }
    }

    private static AdminCategoryUpsert ToUpsert(UpsertAdminCategoryCommand command)
    {
        return new AdminCategoryUpsert(
            command.ParentId,
            AdminCatalogInput.RequireText(command.Name),
            AdminCatalogInput.RequireText(command.Slug),
            AdminCatalogInput.NormalizeText(command.Description),
            AdminCatalogInput.NormalizeText(command.SeoTitle),
            AdminCatalogInput.NormalizeText(command.SeoDescription),
            AdminCatalogInput.NormalizeText(command.H1),
            command.SortOrder ?? 0,
            command.IsActive ?? true,
            command.IsVisibleInMenu ?? true);
    }

    private static AdminCategoryListItemDto ToListItemDto(AdminCategoryRecord record)
    {
        return new AdminCategoryListItemDto(
            record.Id,
            record.ParentId,
            record.Name,
            record.Slug,
            record.SortOrder,
            record.IsActive,
            record.IsVisibleInMenu,
            record.ProductsCount,
            record.ChildrenCount);
    }

    private static AdminCategoryDetailDto ToDetailDto(AdminCategoryRecord record)
    {
        return new AdminCategoryDetailDto(
            record.Id,
            record.ParentId,
            record.Name,
            record.Slug,
            record.Description,
            record.SeoTitle,
            record.SeoDescription,
            record.H1,
            record.SortOrder,
            record.IsActive,
            record.IsVisibleInMenu,
            record.ProductsCount,
            record.ChildrenCount);
    }
}
