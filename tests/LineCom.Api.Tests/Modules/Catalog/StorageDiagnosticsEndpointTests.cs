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

public sealed class StorageDiagnosticsEndpointTests
{
    [Fact]
    public async Task GetDiagnostics_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory("seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/admin/storage/diagnostics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetDiagnostics_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory("customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/storage/diagnostics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetDiagnostics_AsStaff_ReturnsDiagnostics(string role)
    {
        await using var factory = CreateFactory(role);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/storage/diagnostics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<AdminStorageDiagnosticsResponse>(response);
        Assert.Equal(1, body.Summary.MissingFiles);
        Assert.Empty(body.UntrackedFiles.Items);
        Assert.Empty(body.StaleDeletedRows.Items);
        Assert.Empty(body.OrphanedRows.Items);
    }

    private static WebApplicationFactory<Program> CreateFactory(string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IStorageDiagnosticsService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton<IStorageDiagnosticsService>(
                        new ReturningStorageDiagnosticsService(TestResponse()));
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
                });
            });
    }

    private static AdminStorageDiagnosticsResponse TestResponse()
    {
        return new AdminStorageDiagnosticsResponse(
            new AdminStorageDiagnosticsSummary(
                MissingFiles: 1,
                UntrackedFiles: 0,
                StaleDeletedRows: 0,
                OrphanedRows: 0),
            new AdminStorageDiagnosticsList<AdminStorageDiagnosticsStoredFileItem>(
                new[]
                {
                    new AdminStorageDiagnosticsStoredFileItem(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "storage/products/missing.jpg",
                        "product_image",
                        "active",
                        SizeBytes: 10,
                        Checksum: "checksum",
                        CreatedAt: DateTimeOffset.Parse("2026-05-14T00:00:00Z"))
                },
                Count: 1,
                Truncated: false),
            new AdminStorageDiagnosticsList<AdminStorageDiagnosticsUntrackedFileItem>(
                Array.Empty<AdminStorageDiagnosticsUntrackedFileItem>(),
                Count: 0,
                Truncated: false),
            new AdminStorageDiagnosticsList<AdminStorageDiagnosticsStoredFileItem>(
                Array.Empty<AdminStorageDiagnosticsStoredFileItem>(),
                Count: 0,
                Truncated: false),
            new AdminStorageDiagnosticsList<AdminStorageDiagnosticsOrphanedRowItem>(
                Array.Empty<AdminStorageDiagnosticsOrphanedRowItem>(),
                Count: 0,
                Truncated: false));
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

    private sealed class ReturningStorageDiagnosticsService : IStorageDiagnosticsService
    {
        private readonly AdminStorageDiagnosticsResponse response;

        public ReturningStorageDiagnosticsService(AdminStorageDiagnosticsResponse response)
        {
            this.response = response;
        }

        public Task<AdminStorageDiagnosticsResponse> GetDiagnosticsAsync(
            HttpContext httpContext,
            int? maxItems = null,
            CancellationToken cancellationToken = default)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }

            return Task.FromResult(response);
        }
    }

    private sealed class ReturningCustomerLoginService : ICustomerLoginService
    {
        private readonly CurrentUserDto user;

        public ReturningCustomerLoginService(CurrentUserDto user)
        {
            this.user = user;
        }

        public Task<CurrentUserDto> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user);
        }
    }

    private sealed class TestUserLoginRepository : IUserLoginRepository
    {
        private readonly string role;

        public TestUserLoginRepository(string role)
        {
            this.role = role;
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
                role,
                IsActive: true));
        }
    }
}
