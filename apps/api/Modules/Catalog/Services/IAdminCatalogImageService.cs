using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogImageService
{
    Task<AdminProductImagesResponse> GetProductImagesAsync(HttpContext httpContext, Guid productId, CancellationToken cancellationToken = default);
    Task<AdminProductImagesResponse> UploadProductImagesAsync(HttpContext httpContext, Guid productId, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken = default);
    Task<AdminProductImageDto> UpdateProductImageAsync(HttpContext httpContext, Guid productId, Guid imageId, UpdateAdminProductImageCommand command, CancellationToken cancellationToken = default);
    Task<AdminProductImagesResponse> UpdateProductImageOrderAsync(HttpContext httpContext, Guid productId, UpdateAdminProductImageOrderCommand command, CancellationToken cancellationToken = default);
    Task<AdminProductImageDto> SetMainProductImageAsync(HttpContext httpContext, Guid productId, Guid imageId, CancellationToken cancellationToken = default);
    Task DeleteProductImageAsync(HttpContext httpContext, Guid productId, Guid imageId, CancellationToken cancellationToken = default);
}
