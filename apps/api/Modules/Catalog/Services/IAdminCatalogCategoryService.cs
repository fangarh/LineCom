using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogCategoryService
{
    Task<AdminCategoryListResponse> GetCategoriesAsync(
        HttpContext httpContext,
        AdminCategoryListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetailDto> GetCategoryAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetailDto> CreateCategoryAsync(
        HttpContext httpContext,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetailDto> UpdateCategoryAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetailDto> MoveCategoryAsync(
        HttpContext httpContext,
        Guid id,
        MoveAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetailDto> SortCategoryAsync(
        HttpContext httpContext,
        Guid id,
        SortAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);
}
