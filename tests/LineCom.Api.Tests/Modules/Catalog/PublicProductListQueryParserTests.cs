using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductListQueryParserTests
{
    [Fact]
    public void Parse_ReturnsDefaults_WhenQueryIsEmpty()
    {
        var query = PublicProductListQueryParser.Parse(new QueryCollection());

        Assert.Null(query.CategorySlug);
        Assert.Equal(1, query.Page);
        Assert.Equal(24, query.PageSize);
        Assert.Equal("category", query.Sort);
        Assert.Empty(query.AttributeFilters);
    }

    [Fact]
    public void Parse_ReturnsSupportedFilters()
    {
        var query = PublicProductListQueryParser.Parse(CreateQuery(
            ("categorySlug", "vitaya-para"),
            ("brandSlug", "linecom"),
            ("availabilityStatus", "in_stock"),
            ("saleUnit", "coil"),
            ("page", "2"),
            ("pageSize", "12"),
            ("sort", "newest")));

        Assert.Equal("vitaya-para", query.CategorySlug);
        Assert.Equal("linecom", query.BrandSlug);
        Assert.Equal("in_stock", query.AvailabilityStatus);
        Assert.Equal("coil", query.SaleUnit);
        Assert.Equal(2, query.Page);
        Assert.Equal(12, query.PageSize);
        Assert.Equal("newest", query.Sort);
    }

    [Fact]
    public void Parse_ReturnsAttributeFilters()
    {
        var query = PublicProductListQueryParser.Parse(CreateQuery(
            ("attribute.conductor-material", "cu"),
            ("attribute.category", "cat-5e")));

        Assert.Equal("cu", query.AttributeFilters["conductor-material"]);
        Assert.Equal("cat-5e", query.AttributeFilters["category"]);
    }

    [Theory]
    [InlineData("page", "0")]
    [InlineData("page", "abc")]
    [InlineData("pageSize", "0")]
    [InlineData("pageSize", "61")]
    [InlineData("pageSize", "abc")]
    public void Parse_ThrowsInvalidPagination_ForBadPagination(string key, string value)
    {
        var exception = Assert.Throws<ApiException>(() => PublicProductListQueryParser.Parse(CreateQuery((key, value))));

        Assert.Equal("catalog.invalid_pagination", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public void Parse_ThrowsInvalidSort_ForUnknownSort()
    {
        var exception = Assert.Throws<ApiException>(() => PublicProductListQueryParser.Parse(CreateQuery(("sort", "price"))));

        Assert.Equal("catalog.invalid_sort", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("attribute.", "cu")]
    [InlineData("attribute.conductor-material", "")]
    [InlineData("attribute.conductor-material", " ")]
    public void Parse_ThrowsInvalidFilter_ForMalformedAttributeFilters(string key, string value)
    {
        var exception = Assert.Throws<ApiException>(() => PublicProductListQueryParser.Parse(CreateQuery((key, value))));

        Assert.Equal("catalog.invalid_filter", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public void Parse_ThrowsInvalidFilter_ForMultipleAttributeFilterValues()
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>(StringComparer.Ordinal)
        {
            ["attribute.conductor-material"] = new(["cu", "cca"])
        });

        var exception = Assert.Throws<ApiException>(() => PublicProductListQueryParser.Parse(query));

        Assert.Equal("catalog.invalid_filter", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private static QueryCollection CreateQuery(params (string Key, string Value)[] values)
    {
        return new QueryCollection(values.ToDictionary(
            value => value.Key,
            value => new StringValues(value.Value),
            StringComparer.Ordinal));
    }
}
