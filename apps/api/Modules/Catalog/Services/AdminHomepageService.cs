using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminHomepageService : IAdminHomepageService
{
    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminHomepageQuery _query;
    private readonly IAdminHomepageRepository _repository;

    public AdminHomepageService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminHomepageQuery query,
        IAdminHomepageRepository repository)
    {
        _staffGuard = staffGuard;
        _query = query;
        _repository = repository;
    }

    public async Task<AdminHomepageSectionsResponse> GetSectionsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        return await _query.GetSectionsAsync(cancellationToken);
    }

    public async Task<AdminHomepageSectionDto> UpdateSectionAsync(
        HttpContext httpContext,
        Guid id,
        UpdateAdminHomepageSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            var section = await _repository.UpdateSectionAsync(
                id,
                new UpdateAdminHomepageSectionCommand(
                    command.Title is null ? null : AdminCatalogInput.RequireText(command.Title),
                    NormalizeItemLimit(command.ItemLimit),
                    command.SortOrder,
                    command.IsActive),
                cancellationToken);

            return section ?? throw SectionNotFound();
        }
        catch (InvalidAdminHomepageMutationException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminHomepageSectionItemDto> CreateItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        CreateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (HasExactlyOneTarget(command.ProductId, command.CategoryId) is false)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        try
        {
            var item = await _repository.InsertItemAsync(
                sectionId,
                new CreateAdminHomepageSectionItemCommand(
                    command.ProductId,
                    command.CategoryId,
                    command.SortOrder,
                    command.IsActive),
                cancellationToken);

            return item ?? throw SectionNotFound();
        }
        catch (InvalidAdminHomepageMutationException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminHomepageSectionsResponse> UpdateItemOrderAsync(
        HttpContext httpContext,
        Guid sectionId,
        UpdateAdminHomepageSectionItemOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (command.ItemIds.Count == 0 || command.ItemIds.Distinct().Count() != command.ItemIds.Count)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        try
        {
            if (!await _repository.SectionExistsAsync(sectionId, cancellationToken))
            {
                throw SectionNotFound();
            }

            var updated = await _repository.UpdateItemOrderAsync(sectionId, command.ItemIds, cancellationToken);
            if (!updated)
            {
                throw ItemNotFound();
            }

            return await _query.GetSectionsAsync(cancellationToken);
        }
        catch (InvalidAdminHomepageMutationException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminHomepageSectionItemDto> UpdateItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        Guid itemId,
        UpdateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            if (!await _repository.SectionExistsAsync(sectionId, cancellationToken))
            {
                throw SectionNotFound();
            }

            var item = await _repository.UpdateItemAsync(sectionId, itemId, command, cancellationToken);

            return item ?? throw ItemNotFound();
        }
        catch (InvalidAdminHomepageMutationException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task DeleteItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (!await _repository.SectionExistsAsync(sectionId, cancellationToken))
        {
            throw SectionNotFound();
        }

        if (!await _repository.DeleteItemAsync(sectionId, itemId, cancellationToken))
        {
            throw ItemNotFound();
        }
    }

    private static int? NormalizeItemLimit(int? value)
    {
        return value is null ? null : Math.Clamp(value.Value, 1, 24);
    }

    private static bool HasExactlyOneTarget(Guid? productId, Guid? categoryId)
    {
        return (productId is null) != (categoryId is null);
    }

    private static ApiException SectionNotFound()
    {
        return new ApiException(
            "admin_catalog.homepage_section_not_found",
            "Секция главной страницы не найдена.",
            StatusCodes.Status404NotFound);
    }

    private static ApiException ItemNotFound()
    {
        return new ApiException(
            "admin_catalog.homepage_item_not_found",
            "Элемент секции главной страницы не найден.",
            StatusCodes.Status404NotFound);
    }
}
