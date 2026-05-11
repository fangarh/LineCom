using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
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

public sealed class AdminCatalogProductsEndpointTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BrandId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task GetProducts_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogProductService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/admin/catalog/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetProducts_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogProductService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/catalog/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetProducts_AsSeller_ReturnsFilteredProducts()
    {
        var productService = new ReturningAdminCatalogProductService();
        await using var factory = CreateFactory(productService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/products?page=2&pageSize=10&categoryId={CategoryId}&brandId={BrandId}&isActive=false&publishStatus=draft&search=cable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminProductListResponse>(response);
        Assert.Equal(2, body.Page);
        Assert.Equal(10, body.PageSize);
        Assert.False(Assert.Single(body.Items).IsActive);
        Assert.NotNull(productService.LastListQuery);
        Assert.Equal(CategoryId, productService.LastListQuery.CategoryId);
        Assert.Equal(BrandId, productService.LastListQuery.BrandId);
        Assert.False(productService.LastListQuery.IsActive);
        Assert.Equal("draft", productService.LastListQuery.PublishStatus);
        Assert.Equal("cable", productService.LastListQuery.Search);
    }

    [Fact]
    public async Task GetProduct_AsSeller_ReturnsProductDetail()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogProductService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/products/{ProductId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminProductDetailDto>(response);
        Assert.Equal(ProductId, body.Id);
        Assert.Equal(CategoryId, body.CategoryId);
        Assert.Equal("Brand", body.BrandName);
        Assert.Equal(1, body.Images.ImagesCount);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), body.Images.MainImageFileId);
    }

    [Fact]
    public async Task PostProduct_WithCsrfToken_ReturnsCreatedProduct()
    {
        var productService = new ReturningAdminCatalogProductService();
        await using var factory = CreateFactory(productService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/catalog/products")
        {
            Content = JsonContent.Create(Command())
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/admin/catalog/products/{ProductId}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Cable", productService.LastUpsertCommand?.Name);
    }

    [Theory]
    [InlineData("POST", "/api/admin/catalog/products")]
    [InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("DELETE", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogProductService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.SendAsync(CreateMutationRequest(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task DuplicateCandidatesRoute_IsNotImplementedInProductCrudTask()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogProductService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/catalog/products/duplicate-candidates");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminCatalogProductService productService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminCatalogProductService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(productService);
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
                });
            });
    }

    private static HttpRequestMessage CreateMutationRequest(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is not "DELETE")
        {
            request.Content = JsonContent.Create(Command());
        }

        return request;
    }

    private static UpsertAdminProductCommand Command()
    {
        return new UpsertAdminProductCommand(
            CategoryId,
            BrandId,
            "Cable",
            "cable",
            "LC-1",
            null,
            "Description",
            "Short",
            "in_stock",
            "coil",
            "305 m",
            "draft",
            true,
            "SEO",
            "SEO description",
            "H1",
            10);
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await ReadJsonAsync<AuthSessionDto>(response);
        return session.CsrfToken;
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await JsonSerializer.DeserializeAsync<T>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return Assert.IsType<T>(body);
    }

    private static CurrentUserDto TestUser(string role)
    {
        return new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            role);
    }

    private sealed class ReturningAdminCatalogProductService : IAdminCatalogProductService
    {
        public AdminProductListQuery? LastListQuery { get; private set; }
        public UpsertAdminProductCommand? LastUpsertCommand { get; private set; }

        public Task<AdminProductListResponse> GetProductsAsync(
            HttpContext httpContext,
            AdminProductListQuery query,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastListQuery = query;

            return Task.FromResult(new AdminProductListResponse(
                [
                    new AdminProductListItemDto(
                        ProductId,
                        "Cable",
                        "cable",
                        "LC-1",
                        null,
                        "Category",
                        "category",
                        "Brand",
                        "draft",
                        IsActive: false,
                        "in_stock",
                        10,
                        new AdminProductReadinessDto(false, []))
                ],
                Page: query.Page ?? 1,
                PageSize: query.PageSize ?? 20,
                TotalItems: 1,
                TotalPages: 1));
        }

        public Task<AdminProductDetailDto> GetProductAsync(
            HttpContext httpContext,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(id));
        }

        public Task<AdminProductDetailDto> CreateProductAsync(
            HttpContext httpContext,
            UpsertAdminProductCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(ProductId));
        }

        public Task<AdminProductDetailDto> UpdateProductAsync(
            HttpContext httpContext,
            Guid id,
            UpsertAdminProductCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(id));
        }

        public Task DeleteProductAsync(
            HttpContext httpContext,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.CompletedTask;
        }

        private static AdminProductDetailDto Detail(Guid id)
        {
            return new AdminProductDetailDto(
                id,
                CategoryId,
                "Category",
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
                "draft",
                true,
                "SEO",
                "SEO description",
                "H1",
                10,
                new AdminProductReadinessDto(false, []),
                new AdminProductImageSummaryDto(1, Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")),
                []);
        }

        private static void RequireStaff(HttpContext httpContext)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }
        }
    }

    private sealed class ReturningCustomerLoginService : ICustomerLoginService
    {
        private readonly CurrentUserDto _user;

        public ReturningCustomerLoginService(CurrentUserDto user)
        {
            _user = user;
        }

        public Task<CurrentUserDto> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }
    }

    private sealed class TestUserLoginRepository : IUserLoginRepository
    {
        private readonly string _role;

        public TestUserLoginRepository(string role)
        {
            _role = role;
        }

        public Task<LoginUser?> FindByEmailOrPhoneAsync(
            string? email,
            string? phone,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentAuthUser?> FindCurrentUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CurrentAuthUser?>(new CurrentAuthUser(
                userId,
                "Ivan Petrov",
                "ivan@example.com",
                "+79000000000",
                _role,
                IsActive: true));
        }
    }
}
