using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogProductService
{
    Task<AdminProductListResponse> GetProductsAsync(
        HttpContext httpContext,
        AdminProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminProductDuplicateCandidatesResponse> FindDuplicateCandidatesAsync(
        HttpContext httpContext,
        AdminProductDuplicateCandidatesQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> GetProductAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> CreateProductAsync(
        HttpContext httpContext,
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> UpdateProductAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> UpdateAttributesAsync(
        HttpContext httpContext,
        Guid id,
        UpdateAdminProductAttributesCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteProductAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default);
}
