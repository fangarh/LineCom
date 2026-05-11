using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductListSqlBuilderTests
{
    [Fact]
    public void Build_ReturnsBaseListingSqlAndPagingParameters_WhenQueryHasNoFilters()
    {
        var sql = PublicProductListSqlBuilder.Build(
            CreateQuery(page: 3, pageSize: 12),
            categoryId: null);

        Assert.Empty(sql.WhereSql);
        Assert.Equal("ORDER BY product.sort_order, product.name, product.slug", sql.OrderBySql);
        Assert.Contains("WHERE product.is_active = TRUE", sql.CommandText);
        Assert.Contains("AND product.publish_status = 'published'", sql.CommandText);
        Assert.Equal(24, sql.Parameters.Get<int>("Offset"));
        Assert.Equal(12, sql.Parameters.Get<int>("PageSize"));
        Assert.Null(sql.Parameters.Get<Guid?>("CategoryId"));
    }

    [Fact]
    public void Build_AddsPublicListFiltersAndParameters_WhenQueryHasSupportedFilters()
    {
        var categoryId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var sql = PublicProductListSqlBuilder.Build(
            CreateQuery(
                categorySlug: "vitaya-para",
                brandSlug: "linecom",
                availabilityStatus: "in_stock",
                saleUnit: "coil",
                sort: PublicProductSortKeys.Name),
            categoryId);

        Assert.Contains("AND product.primary_category_id = @CategoryId", sql.WhereSql);
        Assert.Contains("AND brand.slug = @BrandSlug", sql.WhereSql);
        Assert.Contains("AND product.availability_status = @AvailabilityStatus", sql.WhereSql);
        Assert.Contains("AND product.sale_unit = @SaleUnit", sql.WhereSql);
        Assert.Equal("ORDER BY product.name, product.slug", sql.OrderBySql);
        Assert.Equal(categoryId, sql.Parameters.Get<Guid?>("CategoryId"));
        Assert.Equal("linecom", sql.Parameters.Get<string>("BrandSlug"));
        Assert.Equal("in_stock", sql.Parameters.Get<string>("AvailabilityStatus"));
        Assert.Equal("coil", sql.Parameters.Get<string>("SaleUnit"));
    }

    [Fact]
    public void Build_OrdersAttributeFiltersByCode_ForStableSqlParameters()
    {
        var sql = PublicProductListSqlBuilder.Build(
            CreateQuery(attributeFilters: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["shield"] = "u-utp",
                ["conductor-material"] = "cu"
            }),
            categoryId: null);

        Assert.Contains("@AttributeCode0", sql.WhereSql);
        Assert.Contains("@AttributeOptionSlug0", sql.WhereSql);
        Assert.Contains("@AttributeCode1", sql.WhereSql);
        Assert.Contains("@AttributeOptionSlug1", sql.WhereSql);
        Assert.Equal("conductor-material", sql.Parameters.Get<string>("AttributeCode0"));
        Assert.Equal("cu", sql.Parameters.Get<string>("AttributeOptionSlug0"));
        Assert.Equal("shield", sql.Parameters.Get<string>("AttributeCode1"));
        Assert.Equal("u-utp", sql.Parameters.Get<string>("AttributeOptionSlug1"));
    }

    [Theory]
    [InlineData(PublicProductSortKeys.Category, "ORDER BY product.sort_order, product.name, product.slug")]
    [InlineData(PublicProductSortKeys.Name, "ORDER BY product.name, product.slug")]
    [InlineData(PublicProductSortKeys.Newest, "ORDER BY product.created_at DESC, product.name")]
    public void Build_UsesPublicSortOrder(string sort, string expectedOrderBySql)
    {
        var sql = PublicProductListSqlBuilder.Build(CreateQuery(sort: sort), categoryId: null);

        Assert.Equal(expectedOrderBySql, sql.OrderBySql);
        Assert.Contains(expectedOrderBySql, sql.CommandText);
    }

    [Fact]
    public void Build_ThrowsInvalidSort_WhenSortIsNotWhitelisted()
    {
        var exception = Assert.Throws<ApiException>(() =>
            PublicProductListSqlBuilder.Build(CreateQuery(sort: "price"), categoryId: null));

        Assert.Equal("catalog.invalid_sort", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private static PublicProductListQuery CreateQuery(
        string? categorySlug = null,
        int page = 1,
        int pageSize = 24,
        string sort = PublicProductSortKeys.Category,
        string? brandSlug = null,
        string? availabilityStatus = null,
        string? saleUnit = null,
        IReadOnlyDictionary<string, string>? attributeFilters = null)
    {
        return new PublicProductListQuery(
            categorySlug,
            page,
            pageSize,
            sort,
            brandSlug,
            availabilityStatus,
            saleUnit,
            attributeFilters ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
