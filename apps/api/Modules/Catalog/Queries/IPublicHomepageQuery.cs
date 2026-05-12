using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public interface IPublicHomepageQuery
{
    Task<PublicHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default);
}
