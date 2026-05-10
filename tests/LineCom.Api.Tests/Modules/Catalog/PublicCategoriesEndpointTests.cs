using System.Net;
using System.Text.Json;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicCategoriesEndpointTests
{
    [Fact]
    public async Task GetCategories_ReturnsPublicCategoryTree()
    {
        var responseBody = new PublicCategoryTreeResponse(
        [
            new PublicCategoryTreeItemDto(
                Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1"),
                null,
                "Витая пара",
                "vitaya-para",
                "Витая пара",
                "Краткое описание категории.",
                10,
                true,
                [
                    new PublicCategoryTreeItemDto(
                        Guid.Parse("dcd4f577-6076-4283-b256-30ea0822a3b2"),
                        Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1"),
                        "Кабель U/UTP",
                        "u-utp",
                        "Кабель U/UTP",
                        null,
                        20,
                        true,
                        [])
                ])
        ]);

        await using var factory = CreateFactory(responseBody);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicCategoryTreeResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        var root = Assert.Single(body.Items);
        Assert.Equal("vitaya-para", root.Slug);
        Assert.Equal("Витая пара", root.H1);
        Assert.True(root.IsVisibleInMenu);

        var child = Assert.Single(root.Children);
        Assert.Equal("u-utp", child.Slug);
        Assert.Equal(root.Id, child.ParentId);
    }

    [Fact]
    public async Task GetCategory_ReturnsPublicCategoryDetail()
    {
        var categoryId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var responseBody = new PublicCategoryDetailDto(
            categoryId,
            null,
            "Витая пара",
            "vitaya-para",
            "Кабель витая пара для СКС и сетевой инфраструктуры.",
            "Витая пара",
            new PublicSeoDto(
                "Витая пара купить",
                "Каталог витой пары для сетей связи.",
                "/catalog/vitaya-para"),
            [new PublicBreadcrumbDto("Витая пара", "vitaya-para")]);

        await using var factory = CreateFactory(new StubPublicCategoryQuery(
            new PublicCategoryTreeResponse([]),
            responseBody));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories/vitaya-para");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicCategoryDetailDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal(categoryId, body.Id);
        Assert.Equal("vitaya-para", body.Slug);
        Assert.Equal("Витая пара купить", body.Seo.Title);
        Assert.Equal("/catalog/vitaya-para", body.Seo.CanonicalPath);
        Assert.Equal("vitaya-para", Assert.Single(body.Breadcrumbs).Slug);
    }

    [Fact]
    public async Task GetCategoryFilters_ReturnsPublicCategoryFilters()
    {
        var responseBody = new PublicCategoryFiltersDto(
            new PublicCategorySummaryDto("Витая пара", "vitaya-para"),
            [
                new PublicFilterDto(
                    "conductor-material",
                    "Материал проводника",
                    "select",
                    null,
                    10,
                    [
                        new PublicFilterOptionDto("CU", "cu", 10),
                        new PublicFilterOptionDto("CCA", "cca", 20)
                    ]),
                new PublicFilterDto(
                    "outdoor",
                    "Для улицы",
                    "boolean",
                    null,
                    20,
                    [])
            ]);

        await using var factory = CreateFactory(new StubPublicCategoryQuery(
            new PublicCategoryTreeResponse([]),
            filters: responseBody));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories/vitaya-para/filters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicCategoryFiltersDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal("vitaya-para", body.Category.Slug);
        Assert.Equal(2, body.Filters.Count);

        var selectFilter = body.Filters[0];
        Assert.Equal("conductor-material", selectFilter.Code);
        Assert.Equal("select", selectFilter.Type);
        Assert.Equal(["cu", "cca"], selectFilter.Options.Select(option => option.Slug).ToArray());

        var booleanFilter = body.Filters[1];
        Assert.Equal("outdoor", booleanFilter.Code);
        Assert.Empty(booleanFilter.Options);
    }

    [Fact]
    public async Task GetCatalogFilters_ReturnsGlobalPublicFilters()
    {
        var responseBody = new PublicCatalogFiltersDto(
        [
            new PublicFilterDto(
                "material",
                "Материал",
                "select",
                null,
                30,
                [
                    new PublicFilterOptionDto("Медь", "copper", 10),
                    new PublicFilterOptionDto("CCA", "cca", 20)
                ])
        ]);

        await using var factory = CreateFactory(new StubPublicCategoryQuery(
            new PublicCategoryTreeResponse([]),
            catalogFilters: responseBody));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/filters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicCatalogFiltersDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        var filter = Assert.Single(body.Filters);
        Assert.Equal("material", filter.Code);
        Assert.Equal(["copper", "cca"], filter.Options.Select(option => option.Slug).ToArray());
    }

    [Fact]
    public async Task GetCategoryFilters_MapsNotFound_ToPublicCatalogError()
    {
        await using var factory = CreateFactory(new StubPublicCategoryQuery(
            new PublicCategoryTreeResponse([]),
            filters: null));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories/missing-category/filters");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal("catalog.category_not_found", body.Code);
        Assert.Equal("Категория не найдена.", body.Message);
    }

    [Fact]
    public async Task GetCategory_MapsNotFound_ToPublicCatalogError()
    {
        await using var factory = CreateFactory(new StubPublicCategoryQuery(
            new PublicCategoryTreeResponse([]),
            categoryDetail: null));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories/missing-category");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal("catalog.category_not_found", body.Code);
        Assert.Equal("Категория не найдена.", body.Message);
    }

    [Fact]
    public async Task GetCategories_ReturnsEmptyItems_WhenCatalogHasNoActiveCategories()
    {
        await using var factory = CreateFactory(new PublicCategoryTreeResponse([]));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicCategoryTreeResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task GetCategories_ReturnsJsonCamelCaseFields()
    {
        await using var factory = CreateFactory(new PublicCategoryTreeResponse(
        [
            new PublicCategoryTreeItemDto(
                Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1"),
                null,
                "Витая пара",
                "vitaya-para",
                null,
                null,
                10,
                true,
                [])
        ]));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\"", body);
        Assert.Contains("\"parentId\"", body);
        Assert.Contains("\"sortOrder\"", body);
        Assert.Contains("\"isVisibleInMenu\"", body);
        Assert.DoesNotContain("\"Items\"", body);
        Assert.DoesNotContain("\"ParentId\"", body);
    }

    [Fact]
    public async Task GetCategories_MapsUnexpectedQueryException_ToInternalErrorWithoutLeakingDetails()
    {
        await using var factory = CreateFactory(
            new ThrowingPublicCategoryQuery(new InvalidOperationException("Database password leaked in exception.")));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/catalog/categories");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Database password leaked", responseText);
        Assert.DoesNotContain(nameof(InvalidOperationException), responseText);

        var body = JsonSerializer.Deserialize<ApiErrorResponse>(
            responseText,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal("internal_error", body.Code);
        Assert.Equal("Внутренняя ошибка сервера.", body.Message);
    }

    [Fact]
    public async Task PostCategories_ReturnsMethodNotAllowed()
    {
        await using var factory = CreateFactory(new PublicCategoryTreeResponse([]));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/public/catalog/categories", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(PublicCategoryTreeResponse responseBody)
    {
        return CreateFactory(new StubPublicCategoryQuery(responseBody));
    }

    private static WebApplicationFactory<Program> CreateFactory(IPublicCategoryQuery categoryQuery)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPublicCategoryQuery>();
                    services.AddSingleton(categoryQuery);
                });
            });
    }

    private sealed class StubPublicCategoryQuery : IPublicCategoryQuery
    {
        private readonly PublicCategoryTreeResponse _responseBody;
        private readonly PublicCategoryDetailDto? _categoryDetail;
        private readonly PublicCatalogFiltersDto _catalogFilters;
        private readonly PublicCategoryFiltersDto? _filters;

        public StubPublicCategoryQuery(
            PublicCategoryTreeResponse responseBody,
            PublicCategoryDetailDto? categoryDetail = null,
            PublicCatalogFiltersDto? catalogFilters = null,
            PublicCategoryFiltersDto? filters = null)
        {
            _responseBody = responseBody;
            _categoryDetail = categoryDetail;
            _catalogFilters = catalogFilters ?? new PublicCatalogFiltersDto([]);
            _filters = filters;
        }

        public Task<PublicCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_responseBody);
        }

        public Task<PublicCategoryDetailDto> GetCategoryDetailAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            if (_categoryDetail is not null)
            {
                return Task.FromResult(_categoryDetail);
            }

            throw new ApiException(
                "catalog.category_not_found",
                "Категория не найдена.",
                StatusCodes.Status404NotFound);
        }

        public Task<PublicCatalogFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_catalogFilters);
        }

        public Task<PublicCategoryFiltersDto> GetCategoryFiltersAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            if (_filters is not null)
            {
                return Task.FromResult(_filters);
            }

            throw new ApiException(
                "catalog.category_not_found",
                "Категория не найдена.",
                StatusCodes.Status404NotFound);
        }
    }

    private sealed class ThrowingPublicCategoryQuery : IPublicCategoryQuery
    {
        private readonly Exception _exception;

        public ThrowingPublicCategoryQuery(Exception exception)
        {
            _exception = exception;
        }

        public Task<PublicCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<PublicCategoryDetailDto> GetCategoryDetailAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<PublicCatalogFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<PublicCategoryFiltersDto> GetCategoryFiltersAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
