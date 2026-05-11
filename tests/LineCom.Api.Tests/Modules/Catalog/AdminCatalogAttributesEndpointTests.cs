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

public sealed class AdminCatalogAttributesEndpointTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AttributeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OptionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task GetAttributes_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogAttributeService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/admin/catalog/categories/{CategoryId}/attributes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetAttributes_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogAttributeService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/categories/{CategoryId}/attributes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetAttributes_AsSeller_ReturnsCategoryAttributes()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/categories/{CategoryId}/attributes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminCategoryAttributesResponse>(response);
        var attribute = Assert.Single(body.Items);
        Assert.Equal(AttributeId, attribute.Id);
        Assert.Equal("Voltage", attribute.Name);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
    }

    [Theory]
    [InlineData("POST", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes")]
    [InlineData("PUT", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    [InlineData("DELETE", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    [InlineData("POST", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/options")]
    [InlineData("PUT", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/options/cccccccc-cccc-cccc-cccc-cccccccccccc")]
    [InlineData("DELETE", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/options/cccccccc-cccc-cccc-cccc-cccccccccccc")]
    [InlineData("POST", "/api/admin/catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/attributes/inherit-from-parent")]
    public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogAttributeService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.SendAsync(CreateMutationRequest(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PostAttribute_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/catalog/categories/{CategoryId}/attributes")
        {
            Content = JsonContent.Create(new UpsertAdminCategoryAttributeCommand(
                "Voltage",
                "voltage",
                "select",
                "V",
                true,
                true,
                false,
                true,
                false,
                false,
                10,
                true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/admin/catalog/categories/{CategoryId}/attributes", response.Headers.Location?.AbsolutePath);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal("Voltage", attributeService.LastAttributeCommand?.Name);
    }

    [Fact]
    public async Task PutAttribute_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/{AttributeId}")
        {
            Content = JsonContent.Create(new UpsertAdminCategoryAttributeCommand(
                "Voltage",
                "voltage",
                "select",
                "V",
                true,
                true,
                false,
                true,
                false,
                false,
                10,
                true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal(AttributeId, attributeService.LastAttributeId);
        Assert.Equal("Voltage", attributeService.LastAttributeCommand?.Name);
    }

    [Fact]
    public async Task DeleteAttribute_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/{AttributeId}");
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal(AttributeId, attributeService.LastAttributeId);
        Assert.True(attributeService.DeleteAttributeCalled);
    }

    [Fact]
    public async Task PostOption_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/{AttributeId}/options")
        {
            Content = JsonContent.Create(new UpsertAdminAttributeOptionCommand("220 V", "220-v", "220 v", 10, true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal(AttributeId, attributeService.LastAttributeId);
        Assert.Equal("220 V", attributeService.LastOptionCommand?.Value);
    }

    [Fact]
    public async Task PutOption_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/{AttributeId}/options/{OptionId}")
        {
            Content = JsonContent.Create(new UpsertAdminAttributeOptionCommand("220 V", "220-v", "220 v", 10, true))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal(AttributeId, attributeService.LastAttributeId);
        Assert.Equal(OptionId, attributeService.LastOptionId);
        Assert.Equal("220 V", attributeService.LastOptionCommand?.Value);
    }

    [Fact]
    public async Task DeleteOption_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/{AttributeId}/options/{OptionId}");
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.Equal(AttributeId, attributeService.LastAttributeId);
        Assert.Equal(OptionId, attributeService.LastOptionId);
        Assert.True(attributeService.DeleteOptionCalled);
    }

    [Fact]
    public async Task InheritFromParent_WithCsrfToken_CallsService()
    {
        var attributeService = new ReturningAdminCatalogAttributeService();
        await using var factory = CreateFactory(attributeService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/catalog/categories/{CategoryId}/attributes/inherit-from-parent");
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CategoryId, attributeService.LastCategoryId);
        Assert.True(attributeService.InheritCalled);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminCatalogAttributeService attributeService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminCatalogAttributeService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(attributeService);
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
                });
            });
    }

    private static HttpRequestMessage CreateMutationRequest(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is not "DELETE" && !path.EndsWith("/inherit-from-parent", StringComparison.Ordinal))
        {
            request.Content = path.Contains("/options/", StringComparison.Ordinal) || path.EndsWith("/options", StringComparison.Ordinal)
                ? JsonContent.Create(new UpsertAdminAttributeOptionCommand("220 V", "220-v", "220 v", 10, true))
                : JsonContent.Create(new UpsertAdminCategoryAttributeCommand("Voltage", "voltage", "select", null, null, null, null, null, null, null, null, null));
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

    private sealed class ReturningAdminCatalogAttributeService : IAdminCatalogAttributeService
    {
        public Guid? LastCategoryId { get; private set; }
        public Guid? LastAttributeId { get; private set; }
        public Guid? LastOptionId { get; private set; }
        public UpsertAdminCategoryAttributeCommand? LastAttributeCommand { get; private set; }
        public UpsertAdminAttributeOptionCommand? LastOptionCommand { get; private set; }
        public bool DeleteAttributeCalled { get; private set; }
        public bool DeleteOptionCalled { get; private set; }
        public bool InheritCalled { get; private set; }

        public Task<AdminCategoryAttributesResponse> GetAttributesAsync(
            HttpContext httpContext,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;

            return Task.FromResult(new AdminCategoryAttributesResponse(
                new[]
                {
                    AttributeDto()
                }));
        }

        public Task<AdminCategoryAttributeDto> CreateAttributeAsync(
            HttpContext httpContext,
            Guid categoryId,
            UpsertAdminCategoryAttributeCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeCommand = command;
            return Task.FromResult(AttributeDto());
        }

        public Task<AdminCategoryAttributeDto> UpdateAttributeAsync(
            HttpContext httpContext,
            Guid categoryId,
            Guid attributeId,
            UpsertAdminCategoryAttributeCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeId = attributeId;
            LastAttributeCommand = command;
            return Task.FromResult(AttributeDto());
        }

        public Task DeleteAttributeAsync(
            HttpContext httpContext,
            Guid categoryId,
            Guid attributeId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeId = attributeId;
            DeleteAttributeCalled = true;
            return Task.CompletedTask;
        }

        public Task<AdminAttributeOptionDto> CreateOptionAsync(
            HttpContext httpContext,
            Guid categoryId,
            Guid attributeId,
            UpsertAdminAttributeOptionCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeId = attributeId;
            LastOptionCommand = command;
            return Task.FromResult(OptionDto());
        }

        public Task<AdminAttributeOptionDto> UpdateOptionAsync(
            HttpContext httpContext,
            Guid categoryId,
            Guid attributeId,
            Guid optionId,
            UpsertAdminAttributeOptionCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeId = attributeId;
            LastOptionId = optionId;
            LastOptionCommand = command;
            return Task.FromResult(OptionDto());
        }

        public Task DeleteOptionAsync(
            HttpContext httpContext,
            Guid categoryId,
            Guid attributeId,
            Guid optionId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            LastAttributeId = attributeId;
            LastOptionId = optionId;
            DeleteOptionCalled = true;
            return Task.CompletedTask;
        }

        public Task<InheritAdminCategoryAttributesResponse> InheritFromParentAsync(
            HttpContext httpContext,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastCategoryId = categoryId;
            InheritCalled = true;
            return Task.FromResult(new InheritAdminCategoryAttributesResponse(Added: 1, Skipped: 0));
        }

        private static void RequireStaff(HttpContext httpContext)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }
        }

        private static AdminCategoryAttributeDto AttributeDto()
        {
            return new AdminCategoryAttributeDto(
                AttributeId,
                CategoryId,
                "Voltage",
                "voltage",
                "select",
                "V",
                IsRequired: true,
                IsFilterable: true,
                IsComparable: false,
                IsVisibleInProduct: true,
                IsSeoImportant: false,
                IsUsedInGeneratedName: false,
                SortOrder: 10,
                IsActive: true,
                ProductValuesCount: 2,
                new[] { OptionDto() });
        }

        private static AdminAttributeOptionDto OptionDto()
        {
            return new AdminAttributeOptionDto(
                OptionId,
                "220 V",
                "220-v",
                "220 v",
                SortOrder: 10,
                IsActive: true,
                ProductValuesCount: 1);
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
