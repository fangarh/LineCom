using System.Collections.Frozen;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed record PublicProductListQuery(
    string? CategorySlug,
    int Page,
    int PageSize,
    string Sort,
    string? BrandSlug,
    string? AvailabilityStatus,
    string? SaleUnit,
    IReadOnlyDictionary<string, string> AttributeFilters);

public static class PublicProductListDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 60;
    public const string DefaultSort = PublicProductSortKeys.Category;
}

public static class PublicProductSortKeys
{
    public const string Category = "category";
    public const string Name = "name";
    public const string Newest = "newest";

    private static readonly IReadOnlySet<string> AllSortKeys = new[]
    {
        Category,
        Name,
        Newest
    }.ToFrozenSet(StringComparer.Ordinal);

    public static IReadOnlySet<string> All => AllSortKeys;
}
