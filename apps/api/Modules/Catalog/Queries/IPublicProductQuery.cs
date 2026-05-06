using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public interface IPublicProductQuery
{
    Task<PublicProductListResponse> GetProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<PublicProductDetailDto> GetProductDetailAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
