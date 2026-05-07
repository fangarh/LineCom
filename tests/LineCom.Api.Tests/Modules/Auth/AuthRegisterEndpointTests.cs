using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class AuthRegisterEndpointTests
{
    [Fact]
    public async Task Register_ReturnsCreatedAuthSessionAndSetsHttpOnlyCookie()
    {
        var user = new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");

        await using var factory = CreateFactory(new ReturningCustomerRegistrationService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "Ivan Petrov",
                "ivan@example.com",
                "+7 900 000-00-00",
                "secure-password"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
        var authCookie = Assert.Single(setCookieHeaders, header => header.StartsWith("linecom_auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);

        var body = await JsonSerializer.DeserializeAsync<AuthSessionDto>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal(user.Id, body.User.Id);
        Assert.Equal("Ivan Petrov", body.User.Name);
        Assert.Equal("ivan@example.com", body.User.Email);
        Assert.Equal("+79000000000", body.User.Phone);
        Assert.Equal("customer", body.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.CsrfToken));
    }

    [Fact]
    public async Task Register_ReturnsInvalidContactError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerRegistrationService(AuthErrors.InvalidContact()));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Ivan Petrov", null, null, "secure-password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.invalid_contact", body.Code);
    }

    [Fact]
    public async Task Register_ReturnsDuplicateContactError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerRegistrationService(AuthErrors.UserAlreadyExists()));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Ivan Petrov", "ivan@example.com", null, "secure-password"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await ReadErrorAsync(response);
        Assert.Equal("auth.user_already_exists", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(ICustomerRegistrationService registrationService)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICustomerRegistrationService>();
                    services.AddSingleton(registrationService);
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

    private sealed class ReturningCustomerRegistrationService : ICustomerRegistrationService
    {
        private readonly CurrentUserDto _user;

        public ReturningCustomerRegistrationService(CurrentUserDto user)
        {
            _user = user;
        }

        public Task<CurrentUserDto> RegisterCustomerAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }
    }

    private sealed class ThrowingCustomerRegistrationService : ICustomerRegistrationService
    {
        private readonly Exception _exception;

        public ThrowingCustomerRegistrationService(Exception exception)
        {
            _exception = exception;
        }

        public Task<CurrentUserDto> RegisterCustomerAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
