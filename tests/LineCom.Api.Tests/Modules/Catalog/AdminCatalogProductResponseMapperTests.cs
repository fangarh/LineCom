using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogProductResponseMapperTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BrandId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AttributeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid MainImageFileId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public void ToDetailDto_MapsImagesAttributesAndReadinessIssues()
    {
        var dto = AdminCatalogProductResponseMapper.ToDetailDto(
            ProductDetailRecord(isActive: false),
            [
                AttributeValueRecord(
                    type: "number",
                    valueText: null,
                    valueNumber: null,
                    valueBoolean: null,
                    attributeOptionId: null,
                    isRequired: true,
                    isValidValue: false)
            ]);

        Assert.Equal(ProductId, dto.Id);
        Assert.Equal(CategoryId, dto.CategoryId);
        Assert.Equal(BrandId, dto.BrandId);
        Assert.Equal(2, dto.Images.ImagesCount);
        Assert.Equal(MainImageFileId, dto.Images.MainImageFileId);

        var attribute = Assert.Single(dto.Attributes);
        Assert.Equal(AttributeId, attribute.AttributeId);
        Assert.Equal("conductor_count", attribute.Code);
        Assert.Equal("number", attribute.Type);

        Assert.False(dto.Readiness.CanPublish);
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "product_inactive");
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "missing_required_attribute");
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "invalid_attribute_value");
    }

    [Fact]
    public void ToListItemDto_UsesRepositoryReadinessCounts()
    {
        var dto = AdminCatalogProductResponseMapper.ToListItemDto(
            ProductListRecord(
                categoryIsActive: false,
                missingRequiredAttributeCount: 2,
                invalidAttributeValueCount: 1));

        Assert.Equal(ProductId, dto.Id);
        Assert.Equal("Cable", dto.Name);
        Assert.Equal("Category", dto.CategoryName);
        Assert.Equal("Brand", dto.BrandName);
        Assert.False(dto.Readiness.CanPublish);
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "inactive_category");
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "missing_required_attribute");
        Assert.Contains(dto.Readiness.Issues, issue => issue.Code == "invalid_attribute_value");
    }

    private static AdminProductListRecord ProductListRecord(
        bool categoryIsActive = true,
        int missingRequiredAttributeCount = 0,
        int invalidAttributeValueCount = 0)
    {
        return new AdminProductListRecord(
            ProductId,
            "Cable",
            "cable",
            "LC-1",
            null,
            CategoryId,
            "Category",
            "category",
            categoryIsActive,
            BrandId,
            "Brand",
            "published",
            IsActive: true,
            "in_stock",
            10,
            missingRequiredAttributeCount,
            invalidAttributeValueCount);
    }

    private static AdminProductDetailRecord ProductDetailRecord(bool isActive = true)
    {
        return new AdminProductDetailRecord(
            ProductId,
            CategoryId,
            "Category",
            CategoryIsActive: true,
            BrandId,
            "Brand",
            "Cable",
            "cable",
            "LC-1",
            null,
            "Description",
            "Short",
            "in_stock",
            "coil",
            "305 m",
            "published",
            isActive,
            "SEO",
            "SEO description",
            "H1",
            10,
            ImagesCount: 2,
            MainImageFileId,
            MissingRequiredAttributeCount: 0,
            InvalidAttributeValueCount: 1);
    }

    private static AdminProductAttributeValueRecord AttributeValueRecord(
        string type,
        string? valueText,
        decimal? valueNumber,
        bool? valueBoolean,
        Guid? attributeOptionId,
        bool isRequired,
        bool isValidValue)
    {
        return new AdminProductAttributeValueRecord(
            AttributeId,
            "conductor_count",
            "Conductor count",
            type,
            null,
            valueText,
            valueNumber,
            valueBoolean,
            attributeOptionId,
            null,
            isRequired,
            isValidValue);
    }
}
