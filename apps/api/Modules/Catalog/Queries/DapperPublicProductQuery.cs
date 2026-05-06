using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperPublicProductQuery : IPublicProductQuery
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPublicCatalogReferenceData _referenceData;

    public DapperPublicProductQuery(
        IDbConnectionFactory connectionFactory,
        IPublicCatalogReferenceData referenceData)
    {
        _connectionFactory = connectionFactory;
        _referenceData = referenceData;
    }

    public async Task<PublicProductListResponse> GetProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateFilterCodes(query);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var categoryId = await ResolveCategoryIdAsync(connection, query.CategorySlug, cancellationToken);
        var sql = PublicProductListSqlBuilder.Build(query, categoryId);
        var command = new CommandDefinition(
            sql.CommandText,
            sql.Parameters,
            cancellationToken: cancellationToken);

        using var result = await connection.QueryMultipleAsync(command);
        var totalItems = await result.ReadSingleAsync<int>();
        var rows = (await result.ReadAsync<PublicProductListRow>()).ToArray();

        return PublicProductListResponseBuilder.Build(
            rows,
            query.Page,
            query.PageSize,
            totalItems,
            _referenceData);
    }

    public async Task<PublicProductDetailDto> GetProductDetailAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var result = await connection.QueryMultipleAsync(
            new CommandDefinition(
                PublicProductSql.GetProductDetail,
                new { Slug = slug },
                cancellationToken: cancellationToken));

        var product = await result.ReadSingleOrDefaultAsync<PublicProductDetailRow>();
        var images = (await result.ReadAsync<PublicProductImageRow>()).ToArray();
        var attributes = (await result.ReadAsync<PublicProductAttributeRow>()).ToArray();
        var breadcrumbs = (await result.ReadAsync<PublicProductCategoryBreadcrumbRow>()).ToArray();

        return PublicProductDetailResponseBuilder.Build(
            product,
            images,
            attributes,
            breadcrumbs,
            _referenceData);
    }

    private async Task<Guid?> ResolveCategoryIdAsync(
        Npgsql.NpgsqlConnection connection,
        string? categorySlug,
        CancellationToken cancellationToken)
    {
        if (categorySlug is null)
        {
            return null;
        }

        var categoryId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                PublicProductSql.SelectActiveCategoryIdBySlug,
                new { CategorySlug = categorySlug },
                cancellationToken: cancellationToken));

        return categoryId ?? throw PublicCatalogErrors.CategoryNotFound();
    }

    private void ValidateFilterCodes(PublicProductListQuery query)
    {
        if (query.AvailabilityStatus is not null)
        {
            _referenceData.GetAvailability(query.AvailabilityStatus);
        }

        if (query.SaleUnit is not null)
        {
            _referenceData.GetSaleUnit(query.SaleUnit);
        }
    }

}
