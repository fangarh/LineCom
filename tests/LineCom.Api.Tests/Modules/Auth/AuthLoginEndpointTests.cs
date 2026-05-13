using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class AuthLoginEndpointTests
{
    [Fact]
    public async Task Login_ReturnsOkAuthSessionAndSetsHttpOnlyCookie()
    {
        var user = new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");

        await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));

        var authCookie = Assert.Single(setCookieHeaders, header => header.StartsWith("linecom_auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);

        var body = await ReadAuthSessionAsync(response);
        Assert.Equal(user.Id, body.User.Id);
        Assert.Equal("Ivan Petrov", body.User.Name);
        Assert.Equal("ivan@example.com", body.User.Email);
        Assert.Equal("+79000000000", body.User.Phone);
        Assert.Equal("customer", body.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.CsrfToken));
    }

    [Fact]
    public async Task Login_ReturnsInvalidCredentialsError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerLoginService(AuthErrors.InvalidCredentials()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.invalid_credentials", body.Code);
    }

    [Fact]
    public async Task Login_ReturnsUserInactiveError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerLoginService(AuthErrors.UserInactive()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.user_inactive", body.Code);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsCurrentAuthenticatedUser()
    {
        var user = new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");

        await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadAuthSessionAsync(response);
        Assert.Equal(user.Id, body.User.Id);
        Assert.Equal("Ivan Petrov", body.User.Name);
        Assert.Equal("ivan@example.com", body.User.Email);
        Assert.Equal("+79000000000", body.User.Phone);
        Assert.Equal("customer", body.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.CsrfToken));
    }

    [Fact]
    public async Task Me_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerLoginService(AuthErrors.InvalidCredentials()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task Logout_WithCsrfToken_ReturnsNoContentAndClearsAuthCookie()
    {
        var user = new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");

        await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var session = await ReadAuthSessionAsync(loginResponse);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add(RequireCsrfTokenAttribute.HeaderName, session.CsrfToken);

        using var response = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
        Assert.Contains(setCookieHeaders, header =>
            header.StartsWith("linecom_auth=", StringComparison.Ordinal) &&
            header.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Logout_WithoutCsrfToken_ReturnsForbidden()
    {
        var user = new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");

        await using var factory = CreateFactory(new ReturningCustomerLoginService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ivan@example.com", "secure-password"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var response = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(ICustomerLoginService loginService)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();
                    services.AddSingleton(loginService);
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository());
                });
            });
    }

    private static async Task<AuthSessionDto> ReadAuthSessionAsync(HttpResponseMessage response)
    {
        var body = await JsonSerializer.DeserializeAsync<AuthSessionDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return Assert.IsType<AuthSessionDto>(body);
    }

    private static async Task<ApiErrorResponse> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return Assert.IsType<ApiErrorResponse>(body);
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

    private sealed class ThrowingCustomerLoginService : ICustomerLoginService
    {
        private readonly Exception _exception;

        public ThrowingCustomerLoginService(Exception exception)
        {
            _exception = exception;
        }

        public Task<CurrentUserDto> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }

    private sealed class TestUserLoginRepository : IUserLoginRepository
    {
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
                "customer",
                IsActive: true));
        }
    }
}
