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

public sealed class AdminCatalogBrandLogoEndpointTests
{
    private static readonly Guid BrandId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StoredFileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task PutLogo_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.SendAsync(CreateUploadRequest(csrfToken: "csrf-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutLogo_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var response = await client.SendAsync(CreateUploadRequest(csrfToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PutLogo_AsSellerWithCsrf_ReturnsLogoAndPassesFile()
    {
        var brandService = new ReturningAdminCatalogBrandService();
        await using var factory = CreateFactory(brandService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var response = await client.SendAsync(CreateUploadRequest(csrfToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminBrandLogoDto>(response);
        Assert.Equal(StoredFileId, body.StoredFileId);
        Assert.Equal(BrandId, brandService.LastLogoBrandId);
        Assert.Equal("logo.png", brandService.LastFileName);
        Assert.Equal("image/png", brandService.LastContentType);
    }

    [Fact]
    public async Task DeleteLogo_AsSellerWithCsrf_ReturnsNoContent()
    {
        var brandService = new ReturningAdminCatalogBrandService();
        await using var factory = CreateFactory(brandService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/catalog/brands/{BrandId}/logo");
        request.Headers.Add("X-CSRF-Token", csrfToken);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(BrandId, brandService.LastDeletedLogoBrandId);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task LogoMutations_WithoutCsrfToken_ReturnForbiddenError(string method)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogBrandService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = method == "PUT"
            ? await client.SendAsync(CreateUploadRequest(csrfToken: null))
            : await client.DeleteAsync($"/api/admin/catalog/brands/{BrandId}/logo");

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

    private static HttpRequestMessage CreateUploadRequest(string? csrfToken)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("image-bytes"u8.ToArray())
        {
            Headers =
            {
                ContentType = new("image/png")
            }
        }, "file", "logo.png");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/catalog/brands/{BrandId}/logo")
        {
            Content = content
        };
        if (csrfToken is not null)
        {
            request.Headers.Add("X-CSRF-Token", csrfToken);
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
        public Guid? LastLogoBrandId { get; private set; }
        public Guid? LastDeletedLogoBrandId { get; private set; }
        public string? LastFileName { get; private set; }
        public string? LastContentType { get; private set; }

        public Task<AdminBrandListResponse> GetBrandsAsync(
            HttpContext httpContext,
            AdminBrandListQuery query,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(new AdminBrandListResponse(Array.Empty<AdminBrandListItemDto>(), 1, 20, 0, 0));
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
            return Task.FromResult(Detail(BrandId));
        }

        public Task<AdminBrandDetailDto> UpdateBrandAsync(
            HttpContext httpContext,
            Guid id,
            UpsertAdminBrandCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
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

        public Task<AdminBrandLogoDto> UploadLogoAsync(
            HttpContext httpContext,
            Guid brandId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastLogoBrandId = brandId;
            LastFileName = file.FileName;
            LastContentType = file.ContentType;
            return Task.FromResult(new AdminBrandLogoDto(
                StoredFileId,
                "/storage/brands/admin/logo.png",
                file.FileName,
                file.ContentType,
                file.Length,
                "checksum"));
        }

        public Task DeleteLogoAsync(
            HttpContext httpContext,
            Guid brandId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastDeletedLogoBrandId = brandId;
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
                StoredFileId,
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
