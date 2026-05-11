using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperAdminProductDuplicateQuery : IAdminProductDuplicateQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminProductDuplicateQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminProductDuplicateCandidatesResponse> FindCandidatesAsync(
        AdminProductDuplicateCandidateQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminProductDuplicateCandidateDto>(
            new CommandDefinition(
                AdminProductDuplicateSql.FindCandidates,
                new
                {
                    query.Name,
                    query.CategoryId,
                    query.BrandId,
                    query.Sku,
                    query.ExternalId,
                    query.Slug,
                    query.ExcludeProductId,
                    Limit = Math.Clamp(query.Limit, 1, 25),
                    SimilarityThreshold = Math.Clamp(query.SimilarityThreshold, 0m, 1m)
                },
                cancellationToken: cancellationToken));

        return new AdminProductDuplicateCandidatesResponse(rows.ToArray());
    }
}
