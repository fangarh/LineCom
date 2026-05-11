using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public interface IAdminHomepageQuery
{
    Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default);
}
