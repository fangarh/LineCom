using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageEndpointTests
{
    [Theory]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("seller", HttpStatusCode.OK)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task GetSections_UsesStaffAuthorization(string role, HttpStatusCode expectedStatus)
    {
        using var factory = new LineComWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient(role);

        var response = await client.GetAsync("/api/admin/homepage/sections");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSection_RequiresCsrf()
    {
        using var factory = new LineComWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("seller");

        var response = await client.PutAsJsonAsync(
            $"/api/admin/homepage/sections/{Guid.NewGuid()}",
            new UpdateAdminHomepageSectionCommand("Главные товары", 6, 10, true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSections_AsSeller_SerializesCriticalContractShape()
    {
        using var factory = new LineComWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("seller");

        using var response = await client.GetAsync("/api/admin/homepage/sections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        AssertJsonHasProperties(root, "sections");
        var section = root.GetProperty("sections").EnumerateArray().Single();
        AssertJsonHasProperties(
            section,
            "id",
            "code",
            "title",
            "type",
            "itemLimit",
            "sortOrder",
            "isActive",
            "items");
        Assert.Equal("featured_products", section.GetProperty("code").GetString());
        Assert.Equal("product_list", section.GetProperty("type").GetString());
        Assert.True(section.GetProperty("isActive").GetBoolean());

        var item = section.GetProperty("items").EnumerateArray().Single();
        AssertJsonHasProperties(
            item,
            "id",
            "productId",
            "categoryId",
            "name",
            "slug",
            "secondaryText",
            "sortOrder",
            "isActive",
            "visibilityStatus");
        Assert.Equal("Cable", item.GetProperty("name").GetString());
        Assert.Equal("cable", item.GetProperty("slug").GetString());
        Assert.Equal("visible", item.GetProperty("visibilityStatus").GetString());
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            Assert.True(element.TryGetProperty(name, out _), $"Expected JSON property '{name}'.");
        }
    }
}

internal static class AdminHomepageEndpointTestClientExtensions
{
    public static HttpClient CreateAuthenticatedClient(this LineComWebApplicationFactory factory, string role)
    {
        var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();
                    services.RemoveAll<IAdminHomepageQuery>();
                    services.RemoveAll<IAdminHomepageRepository>();

                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
                    services.AddSingleton<IAdminHomepageQuery>(new StubHomepageQuery());
                    services.AddSingleton<IAdminHomepageRepository>(new StubHomepageRepository());
                });
            })
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest("ivan@example.com", "secure-password"))
            .GetAwaiter()
            .GetResult();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return client;
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

    private sealed class StubHomepageQuery : IAdminHomepageQuery
    {
        public Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminHomepageSectionsResponse(
            [
                new AdminHomepageSectionDto(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "featured_products",
                    "Featured products",
                    "product_list",
                    6,
                    10,
                    true,
                    [
                        new AdminHomepageSectionItemDto(
                            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                            null,
                            "Cable",
                            "cable",
                            "SKU-1",
                            20,
                            true,
                            "visible")
                    ])
            ]));
        }
    }

    private sealed class StubHomepageRepository : IAdminHomepageRepository
    {
        public Task<bool> SectionExistsAsync(Guid sectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<AdminHomepageSectionDto?> UpdateSectionAsync(
            Guid sectionId,
            UpdateAdminHomepageSectionCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminHomepageSectionDto?>(null);
        }

        public Task<AdminHomepageSectionItemDto?> InsertItemAsync(
            Guid sectionId,
            CreateAdminHomepageSectionItemCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminHomepageSectionItemDto?>(null);
        }

        public Task<AdminHomepageSectionItemDto?> UpdateItemAsync(
            Guid sectionId,
            Guid itemId,
            UpdateAdminHomepageSectionItemCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminHomepageSectionItemDto?>(null);
        }

        public Task<bool> UpdateItemOrderAsync(
            Guid sectionId,
            IReadOnlyList<Guid> itemIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteItemAsync(Guid sectionId, Guid itemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
