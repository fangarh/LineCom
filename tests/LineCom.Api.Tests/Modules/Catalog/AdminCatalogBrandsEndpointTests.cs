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

public sealed class AdminCatalogBrandsEndpointTests
{
    private static readonly Guid BrandId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid LogoFileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetBrands_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/admin/catalog/brands");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetBrands_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/catalog/brands");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetBrands_AsSeller_ReturnsFilteredBrands()
    {
        var brandService = new ReturningAdminCatalogBrandService();
        await using var factory = CreateFactory(brandService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/catalog/brands?page=2&pageSize=10&search=cable&isActive=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminBrandListResponse>(response);
        Assert.Equal(2, body.Page);
        Assert.Equal(10, body.PageSize);
        var item = Assert.Single(body.Items);
        Assert.Equal(BrandId, item.Id);
        Assert.Equal("Cablex", item.Name);
        Assert.NotNull(brandService.LastListQuery);
        Assert.Equal(2, brandService.LastListQuery.Page);
        Assert.Equal(10, brandService.LastListQuery.PageSize);
        Assert.Equal("cable", brandService.LastListQuery.Search);
        Assert.True(brandService.LastListQuery.IsActive);
    }

    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetBrand_AsStaff_ReturnsBrandDetail(string role)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), role);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/brands/{BrandId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminBrandDetailDto>(response);
        Assert.Equal(BrandId, body.Id);
        Assert.Equal("Cablex", body.Name);
        Assert.Equal(LogoFileId, body.LogoFileId);
    }

    [Fact]
    public async Task PostBrand_WithCsrfToken_ReturnsCreatedBrand()
    {
        var brandService = new ReturningAdminCatalogBrandService();
        await using var factory = CreateFactory(brandService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/catalog/brands")
        {
            Content = JsonContent.Create(new UpsertAdminBrandCommand(
                "Cablex",
                "cablex",
                "Description",
                "SEO title",
                "SEO description",
                LogoFileId,
                true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/admin/catalog/brands/{BrandId}", response.Headers.Location?.AbsolutePath);

        var body = await ReadJsonAsync<AdminBrandDetailDto>(response);
        Assert.Equal(BrandId, body.Id);
        Assert.Equal("Cablex", brandService.LastUpsertCommand?.Name);
    }

    [Theory]
    [InlineData("POST", "/api/admin/catalog/brands")]
    [InlineData("PUT", "/api/admin/catalog/brands/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("DELETE", "/api/admin/catalog/brands/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.SendAsync(CreateMutationRequest(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminCatalogBrandService brandService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminCatalogBrandService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(brandService);
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
            request.Content = JsonContent.Create(new UpsertAdminBrandCommand(
                "Cablex",
                "cablex",
                null,
                null,
                null,
                null,
                null));
        }

        return request;
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

    private sealed class ReturningAdminCatalogBrandService : IAdminCatalogBrandService
    {
        public AdminBrandListQuery? LastListQuery { get; private set; }
        public UpsertAdminBrandCommand? LastUpsertCommand { get; private set; }

        public Task<AdminBrandListResponse> GetBrandsAsync(
            HttpContext httpContext,
            AdminBrandListQuery query,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastListQuery = query;

            return Task.FromResult(new AdminBrandListResponse(
                new[]
                {
                    new AdminBrandListItemDto(
                        BrandId,
                        "Cablex",
                        "cablex",
                        true,
                        ProductsCount: 3)
                },
                Page: query.Page ?? 1,
                PageSize: query.PageSize ?? 20,
                TotalItems: 1,
                TotalPages: 1));
        }

        public Task<AdminBrandDetailDto> GetBrandAsync(
            HttpContext httpContext,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(id));
        }

        public Task<AdminBrandDetailDto> CreateBrandAsync(
            HttpContext httpContext,
            UpsertAdminBrandCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(BrandId));
        }

        public Task<AdminBrandDetailDto> UpdateBrandAsync(
            HttpContext httpContext,
            Guid id,
            UpsertAdminBrandCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(id));
        }

        public Task<AdminBrandDetailDto> QuickCreateBrandAsync(
            HttpContext httpContext,
            QuickCreateAdminBrandCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(BrandId));
        }

        public Task DeleteBrandAsync(
            HttpContext httpContext,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.CompletedTask;
        }

        private static void RequireStaff(HttpContext httpContext)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }
        }

        private static AdminBrandDetailDto Detail(Guid id)
        {
            return new AdminBrandDetailDto(
                id,
                "Cablex",
                "cablex",
                "Description",
                "SEO title",
                "SEO description",
                LogoFileId,
                true,
                ProductsCount: 3);
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
