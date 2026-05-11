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

public sealed class AdminCatalogCategoriesEndpointTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ParentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetCategories_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogCategoryService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/admin/catalog/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetCategories_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogCategoryService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/catalog/categories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetCategories_AsSeller_ReturnsFilteredCategories()
    {
        var categoryService = new ReturningAdminCatalogCategoryService();
        await using var factory = CreateFactory(categoryService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync(
            $"/api/admin/catalog/categories?page=2&pageSize=10&parentId={ParentId}&search=cable&isActive=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminCategoryListResponse>(response);
        Assert.Equal(2, body.Page);
        Assert.Equal(10, body.PageSize);
        var item = Assert.Single(body.Items);
        Assert.Equal(CategoryId, item.Id);
        Assert.Equal("Cable", item.Name);
        Assert.NotNull(categoryService.LastListQuery);
        Assert.Equal(2, categoryService.LastListQuery.Page);
        Assert.Equal(10, categoryService.LastListQuery.PageSize);
        Assert.Equal(ParentId, categoryService.LastListQuery.ParentId);
        Assert.Equal("cable", categoryService.LastListQuery.Search);
        Assert.True(categoryService.LastListQuery.IsActive);
    }

    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetCategory_AsStaff_ReturnsCategoryDetail(string role)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogCategoryService(), role);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/categories/{CategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminCategoryDetailDto>(response);
        Assert.Equal(CategoryId, body.Id);
        Assert.Equal("Cable", body.Name);
        Assert.Equal("SEO title", body.SeoTitle);
        Assert.Equal("SEO description", body.SeoDescription);
        Assert.Equal("Cable H1", body.H1);
    }

    [Fact]
    public async Task PostCategory_WithCsrfToken_ReturnsCreatedCategory()
    {
        var categoryService = new ReturningAdminCatalogCategoryService();
        await using var factory = CreateFactory(categoryService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/catalog/categories")
        {
            Content = JsonContent.Create(new UpsertAdminCategoryCommand(
                null,
                "Cable",
                "cable",
                "Description",
                "SEO title",
                "SEO description",
                "Cable H1",
                10,
                true,
                true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/admin/catalog/categories/{CategoryId}", response.Headers.Location?.AbsolutePath);

        var body = await ReadJsonAsync<AdminCategoryDetailDto>(response);
        Assert.Equal(CategoryId, body.Id);
        Assert.Equal("Cable", categoryService.LastUpsertCommand?.Name);
    }

    [Theory]
    [InlineData("POST", "/api/admin/catalog/categories")]
    [InlineData("PUT", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("DELETE", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("PUT", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/move")]
    [InlineData("PUT", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/sort")]
    public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogCategoryService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.SendAsync(CreateMutationRequest(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminCatalogCategoryService categoryService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminCatalogCategoryService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(categoryService);
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
            request.Content = path.EndsWith("/move", StringComparison.Ordinal)
                ? JsonContent.Create(new MoveAdminCategoryCommand(null))
                : path.EndsWith("/sort", StringComparison.Ordinal)
                    ? JsonContent.Create(new SortAdminCategoryCommand(20))
                    : JsonContent.Create(new UpsertAdminCategoryCommand(null, "Cable", "cable", null, null, null, null, null, null, null));
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

    private sealed class ReturningAdminCatalogCategoryService : IAdminCatalogCategoryService
    {
        public AdminCategoryListQuery? LastListQuery { get; private set; }
        public UpsertAdminCategoryCommand? LastUpsertCommand { get; private set; }

        public Task<AdminCategoryListResponse> GetCategoriesAsync(
            HttpContext httpContext,
            AdminCategoryListQuery query,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastListQuery = query;

            return Task.FromResult(new AdminCategoryListResponse(
                new[]
                {
                    new AdminCategoryListItemDto(
                        CategoryId,
                        ParentId,
                        "Cable",
                        "cable",
                        10,
                        true,
                        true,
                        ProductsCount: 3,
                        ChildrenCount: 1)
                },
                Page: query.Page ?? 1,
                PageSize: query.PageSize ?? 20,
                TotalItems: 1,
                TotalPages: 1));
        }

        public Task<AdminCategoryDetailDto> GetCategoryAsync(
            HttpContext httpContext,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(id));
        }

        public Task<AdminCategoryDetailDto> CreateCategoryAsync(
            HttpContext httpContext,
            UpsertAdminCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(CategoryId));
        }

        public Task<AdminCategoryDetailDto> UpdateCategoryAsync(
            HttpContext httpContext,
            Guid id,
            UpsertAdminCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastUpsertCommand = command;
            return Task.FromResult(Detail(id));
        }

        public Task<AdminCategoryDetailDto> MoveCategoryAsync(
            HttpContext httpContext,
            Guid id,
            MoveAdminCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(id));
        }

        public Task<AdminCategoryDetailDto> SortCategoryAsync(
            HttpContext httpContext,
            Guid id,
            SortAdminCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(id));
        }

        public Task DeleteCategoryAsync(
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

        private static AdminCategoryDetailDto Detail(Guid id)
        {
            return new AdminCategoryDetailDto(
                id,
                ParentId,
                "Cable",
                "cable",
                "Description",
                "SEO title",
                "SEO description",
                "Cable H1",
                10,
                true,
                true,
                ProductsCount: 3,
                ChildrenCount: 1);
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
