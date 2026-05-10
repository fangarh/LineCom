using System.Net;
using System.Text.Json;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicProductsEndpointTests
{
    [Fact]
    public async Task GetProducts_ReturnsPublicProductList()
    {
        var productId = Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a");
        var responseBody = new PublicProductListResponse(
        [
            new PublicProductListItemDto(
                productId,
                "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                "u-utp-cat-5e-cu-305m",
                "LC-UTP5E-CU-305",
                new PublicBrandSummaryDto("LineCom", "linecom"),
                new PublicCategorySummaryDto("Витая пара", "vitaya-para"),
                new PublicCodeLabelDto("in_stock", "В наличии"),
                new PublicCodeLabelDto("coil", "бухта"),
                "305 м",
                new PublicImageDto(
                    "/storage/products/u-utp-cat-5e-cu-305m.jpg",
                    "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                    null))
        ],
        Page: 1,
        PageSize: 24,
        TotalItems: 1,
        TotalPages: 1);

        await using var factory = CreateFactory(new CapturingPublicProductQuery(responseBody));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/public/catalog/products?categorySlug=vitaya-para&sort=name&page=1&pageSize=24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicProductListResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(24, body.PageSize);
        Assert.Equal(1, body.TotalItems);

        var product = Assert.Single(body.Items);
        Assert.Equal(productId, product.Id);
        Assert.Equal("u-utp-cat-5e-cu-305m", product.Slug);
        Assert.Equal("linecom", product.Brand?.Slug);
        Assert.Equal("vitaya-para", product.Category.Slug);
        Assert.Equal("in_stock", product.Availability.Code);
        Assert.Equal("coil", product.SaleUnit.Code);
        Assert.Equal("/storage/products/u-utp-cat-5e-cu-305m.jpg", product.MainImage?.Url);
    }

    [Fact]
    public async Task GetProduct_ReturnsPublicProductDetail()
    {
        var productId = Guid.Parse("e9c9e401-2f72-49a6-95bd-4e649cedeb3a");
        var responseBody = new PublicProductDetailDto(
            productId,
            "Кабель U/UTP Cat 5e 4 пары CU 305 м",
            "u-utp-cat-5e-cu-305m",
            "LC-UTP5E-CU-305",
            "Описание товара.",
            "Кабель для структурированных кабельных систем.",
            "Кабель U/UTP Cat 5e 4 пары CU 305 м",
            new PublicCategorySummaryDto("Витая пара", "vitaya-para"),
            new PublicBrandSummaryDto("LineCom", "linecom"),
            new PublicCodeLabelDto("in_stock", "В наличии"),
            new PublicCodeLabelDto("coil", "бухта"),
            "305 м",
            [
                new PublicImageDto(
                    "/storage/products/u-utp-cat-5e-cu-305m.jpg",
                    "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                    null)
            ],
            [
                new PublicProductAttributeDto(
                    "conductor-material",
                    "Материал проводника",
                    "select",
                    null,
                    "CU",
                    10)
            ],
            new PublicSeoDto(
                "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                "Купить кабель U/UTP Cat 5e для СКС.",
                "/catalog/products/u-utp-cat-5e-cu-305m"),
            [
                new PublicBreadcrumbDto("Витая пара", "vitaya-para"),
                new PublicBreadcrumbDto("Кабель U/UTP Cat 5e 4 пары CU 305 м", "u-utp-cat-5e-cu-305m")
            ]);

        await using var factory = CreateFactory(new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0),
            responseBody));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products/u-utp-cat-5e-cu-305m");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicProductDetailDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal(productId, body.Id);
        Assert.Equal("u-utp-cat-5e-cu-305m", body.Slug);
        Assert.Equal("vitaya-para", body.Category.Slug);
        Assert.Equal("linecom", body.Brand?.Slug);
        Assert.Equal("in_stock", body.Availability.Code);
        Assert.Equal("coil", body.SaleUnit.Code);
        Assert.Equal("/catalog/products/u-utp-cat-5e-cu-305m", body.Seo.CanonicalPath);
        Assert.Equal("vitaya-para", body.Breadcrumbs[0].Slug);
        Assert.Equal("u-utp-cat-5e-cu-305m", body.Breadcrumbs[1].Slug);
    }

    [Fact]
    public async Task GetProduct_ReturnsProductNotFound_WhenQueryRejectsSlug()
    {
        await using var factory = CreateFactory(new ThrowingPublicProductQuery(PublicCatalogErrors.ProductNotFound()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products/missing-product");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.product_not_found", body.Code);
        Assert.Equal("Товар не найден.", body.Message);
    }

    [Fact]
    public async Task GetProducts_UsesDefaultPaginationAndSort_WhenQueryIsEmpty()
    {
        var productQuery = new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0));

        await using var factory = CreateFactory(productQuery);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(productQuery.LastQuery);
        Assert.Null(productQuery.LastQuery.CategorySlug);
        Assert.Equal(PublicProductListDefaults.DefaultPage, productQuery.LastQuery.Page);
        Assert.Equal(PublicProductListDefaults.DefaultPageSize, productQuery.LastQuery.PageSize);
        Assert.Equal(PublicProductListDefaults.DefaultSort, productQuery.LastQuery.Sort);
    }

    [Fact]
    public async Task GetProducts_PassesAttributeFiltersToQuery()
    {
        var productQuery = new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0));

        await using var factory = CreateFactory(productQuery);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/public/catalog/products?categorySlug=vitaya-para&attribute.conductor-material=cu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(productQuery.LastQuery);
        Assert.Equal("vitaya-para", productQuery.LastQuery.CategorySlug);
        Assert.Equal("cu", productQuery.LastQuery.AttributeFilters["conductor-material"]);
    }

    [Fact]
    public async Task GetProducts_ReturnsInvalidPagination_WhenPageIsInvalid()
    {
        await using var factory = CreateFactory(new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0)));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.invalid_pagination", body.Code);
        Assert.Equal("Некорректные параметры пагинации.", body.Message);
    }

    [Fact]
    public async Task GetProducts_ReturnsInvalidSort_WhenSortIsUnknown()
    {
        await using var factory = CreateFactory(new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0)));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products?sort=price");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.invalid_sort", body.Code);
        Assert.Equal("Некорректный параметр сортировки.", body.Message);
    }

    [Fact]
    public async Task GetProducts_ReturnsInvalidFilter_WhenAttributeFilterIsMalformed()
    {
        await using var factory = CreateFactory(new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0)));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products?attribute.=cu");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.invalid_filter", body.Code);
        Assert.Equal("Некорректный параметр фильтра.", body.Message);
    }

    [Fact]
    public async Task GetProducts_ReturnsInvalidFilter_WhenAvailabilityStatusIsUnknown()
    {
        await using var factory = CreateFactory(new ThrowingPublicProductQuery(PublicCatalogErrors.InvalidFilter()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products?availabilityStatus=unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.invalid_filter", body.Code);
        Assert.Equal("Некорректный параметр фильтра.", body.Message);
    }

    [Fact]
    public async Task GetProducts_ReturnsCategoryNotFound_WhenQueryRejectsCategorySlug()
    {
        await using var factory = CreateFactory(new ThrowingPublicProductQuery(PublicCatalogErrors.CategoryNotFound()));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/products?categorySlug=missing-category");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("catalog.category_not_found", body.Code);
        Assert.Equal("Категория не найдена.", body.Message);
    }

    [Fact]
    public async Task PostProducts_ReturnsMethodNotAllowed()
    {
        await using var factory = CreateFactory(new CapturingPublicProductQuery(
            new PublicProductListResponse([], 1, 24, 0, 0)));

        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/public/catalog/products", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(IPublicProductQuery productQuery)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPublicProductQuery>();
                    services.AddSingleton(productQuery);
                });
            });
    }

    private static async Task<ApiErrorResponse> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return Assert.IsType<ApiErrorResponse>(body);
    }

    private sealed class CapturingPublicProductQuery : IPublicProductQuery
    {
        private readonly PublicProductListResponse _responseBody;
        private readonly PublicProductDetailDto? _detailResponseBody;

        public CapturingPublicProductQuery(
            PublicProductListResponse responseBody,
            PublicProductDetailDto? detailResponseBody = null)
        {
            _responseBody = responseBody;
            _detailResponseBody = detailResponseBody;
        }

        public PublicProductListQuery? LastQuery { get; private set; }

        public Task<PublicProductListResponse> GetProductsAsync(
            PublicProductListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(_responseBody);
        }

        public Task<PublicProductDetailDto> GetProductDetailAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            if (_detailResponseBody is not null)
            {
                return Task.FromResult(_detailResponseBody);
            }

            throw PublicCatalogErrors.ProductNotFound();
        }
    }

    private sealed class ThrowingPublicProductQuery : IPublicProductQuery
    {
        private readonly Exception _exception;

        public ThrowingPublicProductQuery(Exception exception)
        {
            _exception = exception;
        }

        public Task<PublicProductListResponse> GetProductsAsync(
            PublicProductListQuery query,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<PublicProductDetailDto> GetProductDetailAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
