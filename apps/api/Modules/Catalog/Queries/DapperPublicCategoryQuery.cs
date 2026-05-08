using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperPublicCategoryQuery : IPublicCategoryQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperPublicCategoryQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PublicCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PublicCategoryRow>(
            new CommandDefinition(PublicCategorySql.GetActiveCategories, cancellationToken: cancellationToken));
        var items = PublicCategoryTreeBuilder.Build(rows.ToArray());

        return new PublicCategoryTreeResponse(items);
    }

    public async Task<PublicCategoryDetailDto> GetCategoryDetailAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PublicCategoryDetailRow>(
            new CommandDefinition(
                PublicCategorySql.GetActiveCategoryBreadcrumbs,
                new { Slug = slug },
                cancellationToken: cancellationToken));

        return PublicCategoryDetailBuilder.Build(rows.ToArray());
    }

    public async Task<PublicCatalogFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PublicCategoryFilterRow>(
            new CommandDefinition(PublicCategorySql.GetActiveCatalogFilters, cancellationToken: cancellationToken));

        return new PublicCatalogFiltersDto(PublicCategoryFiltersBuilder.BuildFilters(rows.ToArray()));
    }

    public async Task<PublicCategoryFiltersDto> GetCategoryFiltersAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var result = await connection.QueryMultipleAsync(
            new CommandDefinition(
                PublicCategorySql.GetActiveCategoryFilters,
                new { Slug = slug },
                cancellationToken: cancellationToken));

        var category = await result.ReadSingleOrDefaultAsync<PublicCategoryFilterCategoryRow>();
        var rows = (await result.ReadAsync<PublicCategoryFilterRow>()).ToArray();

        return PublicCategoryFiltersBuilder.Build(category, rows);
    }
}
