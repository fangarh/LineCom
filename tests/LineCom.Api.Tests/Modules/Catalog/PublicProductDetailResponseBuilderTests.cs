using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductDetailResponseBuilderTests
{
    [Fact]
    public void Build_ReturnsProductDetailWithImagesAttributesSeoAndBreadcrumbs()
    {
        var productId = Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a");

        var detail = PublicProductDetailResponseBuilder.Build(
            CreateProduct(productId),
            [
                new PublicProductImageRow(
                    "/storage/products/u-utp-cat-5e-cu-305m.jpg",
                    "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                    null)
            ],
            [
                CreateAttribute("conductor-material", "Материал проводника", "select", optionValue: "CU", sortOrder: 10),
                CreateAttribute("pairs", "Количество пар", "number", valueNumber: 4, sortOrder: 20),
                CreateAttribute("outdoor", "Для улицы", "boolean", valueBoolean: true, sortOrder: 30),
                CreateAttribute("jacket", "Оболочка", "text", valueText: "PVC", sortOrder: 40)
            ],
            [new PublicProductCategoryBreadcrumbRow("Витая пара", "vitaya-para", Depth: 0)],
            new PublicCatalogReferenceData());

        Assert.Equal(productId, detail.Id);
        Assert.Equal("linecom", detail.Brand?.Slug);
        Assert.Equal("vitaya-para", detail.Category.Slug);
        Assert.Equal("В наличии", detail.Availability.Label);
        Assert.Equal("бухта", detail.SaleUnit.Label);
        Assert.Equal("/catalog/products/u-utp-cat-5e-cu-305m", detail.Seo.CanonicalPath);
        Assert.Equal("/storage/products/u-utp-cat-5e-cu-305m.jpg", Assert.Single(detail.Images).Url);
        Assert.Equal(["vitaya-para", "u-utp-cat-5e-cu-305m"], detail.Breadcrumbs.Select(item => item.Slug).ToArray());

        Assert.Equal("CU", detail.Attributes[0].Value);
        Assert.Equal(4m, detail.Attributes[1].Value);
        Assert.Equal(true, detail.Attributes[2].Value);
        Assert.Equal("PVC", detail.Attributes[3].Value);
    }

    [Fact]
    public void Build_ReturnsNullBrand_WhenBrandIsNotPublic()
    {
        var product = CreateProduct(
            Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a"),
            brandName: null,
            brandSlug: null);

        var detail = PublicProductDetailResponseBuilder.Build(
            product,
            [],
            [],
            [new PublicProductCategoryBreadcrumbRow("Витая пара", "vitaya-para", Depth: 0)],
            new PublicCatalogReferenceData());

        Assert.Null(detail.Brand);
    }

    [Fact]
    public void Build_ThrowsProductNotFound_WhenProductRowIsMissing()
    {
        var exception = Assert.Throws<ApiException>(() => PublicProductDetailResponseBuilder.Build(
            product: null,
            [],
            [],
            [],
            new PublicCatalogReferenceData()));

        Assert.Equal("catalog.product_not_found", exception.Code);
        Assert.Equal("Товар не найден.", exception.Message);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    [Fact]
    public void Build_ThrowsInternalError_WhenAttributeValueDoesNotMatchType()
    {
        var product = CreateProduct(Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a"));
        var attribute = CreateAttribute("pairs", "Количество пар", "number");

        var exception = Assert.Throws<InvalidOperationException>(() => PublicProductDetailResponseBuilder.Build(
            product,
            [],
            [attribute],
            [],
            new PublicCatalogReferenceData()));

        Assert.Equal("Invalid public product attribute value for 'pairs'.", exception.Message);
    }

    private static PublicProductDetailRow CreateProduct(
        Guid id,
        string? brandName = "LineCom",
        string? brandSlug = "linecom")
    {
        return new PublicProductDetailRow(
            id,
            "Кабель U/UTP Cat 5e 4 пары CU 305 м",
            "u-utp-cat-5e-cu-305m",
            "LC-UTP5E-CU-305",
            "Описание товара.",
            "Кабель для структурированных кабельных систем.",
            "Кабель U/UTP Cat 5e 4 пары CU 305 м",
            "Витая пара",
            "vitaya-para",
            brandName,
            brandSlug,
            "in_stock",
            "coil",
            "305 м",
            "Кабель U/UTP Cat 5e 4 пары CU 305 м",
            "Купить кабель U/UTP Cat 5e для СКС.");
    }

    private static PublicProductAttributeRow CreateAttribute(
        string code,
        string name,
        string type,
        string? valueText = null,
        decimal? valueNumber = null,
        bool? valueBoolean = null,
        string? optionValue = null,
        int sortOrder = 10)
    {
        return new PublicProductAttributeRow(
            code,
            name,
            type,
            Unit: null,
            valueText,
            valueNumber,
            valueBoolean,
            optionValue,
            sortOrder);
    }
}
