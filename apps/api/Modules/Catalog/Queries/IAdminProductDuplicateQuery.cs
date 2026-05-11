using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed record AdminProductDuplicateCandidateQuery(
    string? Name,
    Guid? CategoryId,
    Guid? BrandId,
    string? Sku,
    string? ExternalId,
    string? Slug,
    Guid? ExcludeProductId,
    int Limit = 10,
    decimal SimilarityThreshold = 0.35m);

public interface IAdminProductDuplicateQuery
{
    Task<AdminProductDuplicateCandidatesResponse> FindCandidatesAsync(
        AdminProductDuplicateCandidateQuery query,
        CancellationToken cancellationToken = default);
}
