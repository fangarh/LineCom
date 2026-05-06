using Dapper;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicProductListSqlBuilder
{
    public static PublicProductListSql Build(PublicProductListQuery query, Guid? categoryId)
    {
        var sqlParts = BuildSqlParts(query);

        return new PublicProductListSql(
            PublicProductSql.BuildProductListSql(sqlParts.WhereSql, sqlParts.OrderBySql),
            BuildParameters(query, categoryId),
            sqlParts.WhereSql,
            sqlParts.OrderBySql);
    }

    private static ProductListSqlParts BuildSqlParts(PublicProductListQuery query)
    {
        var whereClauses = new List<string>();

        if (query.CategorySlug is not null)
        {
            whereClauses.Add("AND product.primary_category_id = @CategoryId");
        }

        if (query.BrandSlug is not null)
        {
            whereClauses.Add("AND brand.slug = @BrandSlug");
        }

        if (query.AvailabilityStatus is not null)
        {
            whereClauses.Add("AND product.availability_status = @AvailabilityStatus");
        }

        if (query.SaleUnit is not null)
        {
            whereClauses.Add("AND product.sale_unit = @SaleUnit");
        }

        var attributeFilterIndex = 0;
        foreach (var _ in query.AttributeFilters.OrderBy(filter => filter.Key, StringComparer.Ordinal))
        {
            whereClauses.Add(PublicProductSql.BuildSelectAttributeFilterSql(attributeFilterIndex));
            attributeFilterIndex++;
        }

        var whereSql = whereClauses.Count == 0
            ? string.Empty
            : Environment.NewLine + string.Join(Environment.NewLine, whereClauses);

        return new ProductListSqlParts(whereSql, GetOrderBySql(query.Sort));
    }

    private static DynamicParameters BuildParameters(PublicProductListQuery query, Guid? categoryId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CategoryId", categoryId);
        parameters.Add("BrandSlug", query.BrandSlug);
        parameters.Add("AvailabilityStatus", query.AvailabilityStatus);
        parameters.Add("SaleUnit", query.SaleUnit);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);
        parameters.Add("PageSize", query.PageSize);

        var attributeFilterIndex = 0;
        foreach (var filter in query.AttributeFilters.OrderBy(filter => filter.Key, StringComparer.Ordinal))
        {
            parameters.Add($"AttributeCode{attributeFilterIndex}", filter.Key);
            parameters.Add($"AttributeOptionSlug{attributeFilterIndex}", filter.Value);
            attributeFilterIndex++;
        }

        return parameters;
    }

    private static string GetOrderBySql(string sort)
    {
        return sort switch
        {
            PublicProductSortKeys.Category => "ORDER BY product.sort_order, product.name, product.slug",
            PublicProductSortKeys.Name => "ORDER BY product.name, product.slug",
            PublicProductSortKeys.Newest => "ORDER BY product.created_at DESC, product.name",
            _ => throw PublicCatalogErrors.InvalidSort()
        };
    }

    private sealed record ProductListSqlParts(string WhereSql, string OrderBySql);
}

internal sealed record PublicProductListSql(
    string CommandText,
    DynamicParameters Parameters,
    string WhereSql,
    string OrderBySql);
