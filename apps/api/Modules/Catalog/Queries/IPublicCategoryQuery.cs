using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public interface IPublicCategoryQuery
{
    Task<PublicCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default);

    Task<PublicCategoryDetailDto> GetCategoryDetailAsync(string slug, CancellationToken cancellationToken = default);

    Task<PublicCatalogFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default);

    Task<PublicCategoryFiltersDto> GetCategoryFiltersAsync(string slug, CancellationToken cancellationToken = default);
}
