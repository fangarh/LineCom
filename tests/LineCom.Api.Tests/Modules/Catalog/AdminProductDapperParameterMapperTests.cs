using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminProductDapperParameterMapperTests
{
    [Fact]
    public void ToUpsertParameters_MapsProductUpsertForDapper()
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var brandId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var command = new AdminProductUpsert(
            categoryId,
            brandId,
            "Linecom Cable",
            "linecom-cable",
            "LC-1",
            "EXT-1",
            "Full description",
            "Short description",
            "in_stock",
            "meter",
            "100",
            "published",
            true,
            "SEO title",
            "SEO description",
            "H1 title",
            10);

        var parameters = AdminProductDapperParameterMapper.ToUpsertParameters(command, productId);

        Assert.Equal(productId, parameters.Id);
        Assert.Equal(categoryId, parameters.CategoryId);
        Assert.Equal(brandId, parameters.BrandId);
        Assert.Equal("Linecom Cable", parameters.Name);
        Assert.Equal("linecom-cable", parameters.Slug);
        Assert.Equal("LC-1", parameters.Sku);
        Assert.Equal("EXT-1", parameters.ExternalId);
        Assert.Equal("Full description", parameters.Description);
        Assert.Equal("Short description", parameters.ShortDescription);
        Assert.Equal("in_stock", parameters.AvailabilityStatus);
        Assert.Equal("meter", parameters.SaleUnit);
        Assert.Equal("100", parameters.UnitQuantity);
        Assert.Equal("published", parameters.PublishStatus);
        Assert.True(parameters.IsActive);
        Assert.Equal("SEO title", parameters.SeoTitle);
        Assert.Equal("SEO description", parameters.SeoDescription);
        Assert.Equal("H1 title", parameters.H1);
        Assert.Equal(10, parameters.SortOrder);
    }

    [Fact]
    public void ToAttributeValueParameters_MapsAttributeValueUpsertForDapper()
    {
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var attributeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var optionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var command = new AdminProductAttributeValueUpsert(
            attributeId,
            "value",
            12.5m,
            true,
            optionId);

        var parameters = AdminProductDapperParameterMapper.ToAttributeValueParameters(productId, command);

        Assert.Equal(productId, parameters.ProductId);
        Assert.Equal(attributeId, parameters.AttributeId);
        Assert.Equal("value", parameters.ValueText);
        Assert.Equal(12.5m, parameters.ValueNumber);
        Assert.True(parameters.ValueBoolean);
        Assert.Equal(optionId, parameters.AttributeOptionId);
    }
}
