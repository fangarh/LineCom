using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicProductListQueryParser
{
    public static PublicProductListQuery Parse(IQueryCollection query)
    {
        var page = ParseOptionalInt(query, "page", PublicProductListDefaults.DefaultPage);
        var pageSize = ParseOptionalInt(query, "pageSize", PublicProductListDefaults.DefaultPageSize);

        if (page < 1 || pageSize < 1 || pageSize > PublicProductListDefaults.MaxPageSize)
        {
            throw PublicCatalogErrors.InvalidPagination();
        }

        var sort = GetOptionalValue(query, "sort") ?? PublicProductListDefaults.DefaultSort;
        if (!PublicProductSortKeys.All.Contains(sort))
        {
            throw PublicCatalogErrors.InvalidSort();
        }

        var attributeFilters = query
            .Where(parameter => parameter.Key.StartsWith("attribute.", StringComparison.Ordinal))
            .ToDictionary(
                parameter => parameter.Key["attribute.".Length..],
                parameter => GetRequiredAttributeFilterValue(parameter.Key, parameter.Value),
                StringComparer.Ordinal);

        return new PublicProductListQuery(
            GetOptionalValue(query, "categorySlug"),
            page,
            pageSize,
            sort,
            GetOptionalValue(query, "brandSlug"),
            GetOptionalValue(query, "availabilityStatus"),
            GetOptionalValue(query, "saleUnit"),
            attributeFilters);
    }

    private static string GetRequiredAttributeFilterValue(
        string parameterName,
        Microsoft.Extensions.Primitives.StringValues values)
    {
        var attributeCode = parameterName["attribute.".Length..];
        if (string.IsNullOrWhiteSpace(attributeCode) || values.Count != 1)
        {
            throw PublicCatalogErrors.InvalidFilter();
        }

        var value = values.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw PublicCatalogErrors.InvalidFilter();
        }

        return value;
    }

    private static int ParseOptionalInt(IQueryCollection query, string key, int defaultValue)
    {
        var value = GetOptionalValue(query, key);
        if (value is null)
        {
            return defaultValue;
        }

        return int.TryParse(value, out var parsedValue)
            ? parsedValue
            : throw PublicCatalogErrors.InvalidPagination();
    }

    private static string? GetOptionalValue(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return null;
        }

        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
