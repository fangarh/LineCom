using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductListResponseBuilderTests
{
    [Fact]
    public void Build_ReturnsNullBrandAndMainImage_WhenPublicRelationsAreMissing()
    {
        var response = PublicProductListResponseBuilder.Build(
        [
            new PublicProductListRow(
                Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a"),
                "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                "u-utp-cat-5e-cu-305m",
                "LC-UTP5E-CU-305",
                BrandName: null,
                BrandSlug: null,
                CategoryName: "Витая пара",
                CategorySlug: "vitaya-para",
                AvailabilityStatus: "in_stock",
                SaleUnit: "coil",
                UnitQuantity: "305 м",
                MainImageUrl: null,
                MainImageAlt: null,
                MainImageTitle: null)
        ],
        page: 1,
        pageSize: 24,
        totalItems: 1,
        referenceData: new PublicCatalogReferenceData());

        var product = Assert.Single(response.Items);
        Assert.Null(product.Brand);
        Assert.Null(product.MainImage);
        Assert.Equal("В наличии", product.Availability.Label);
        Assert.Equal("бухта", product.SaleUnit.Label);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public void Build_ReturnsZeroTotalPages_WhenTotalItemsIsZero()
    {
        var response = PublicProductListResponseBuilder.Build(
            [],
            page: 1,
            pageSize: 24,
            totalItems: 0,
            referenceData: new PublicCatalogReferenceData());

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalPages);
    }
}
