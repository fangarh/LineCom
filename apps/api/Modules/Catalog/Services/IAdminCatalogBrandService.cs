using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogBrandService
{
    Task<AdminBrandListResponse> GetBrandsAsync(
        HttpContext httpContext,
        AdminBrandListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetailDto> GetBrandAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetailDto> CreateBrandAsync(
        HttpContext httpContext,
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetailDto> UpdateBrandAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetailDto> QuickCreateBrandAsync(
        HttpContext httpContext,
        QuickCreateAdminBrandCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteBrandAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminBrandLogoDto> UploadLogoAsync(
        HttpContext httpContext,
        Guid brandId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteLogoAsync(
        HttpContext httpContext,
        Guid brandId,
        CancellationToken cancellationToken = default);
}
