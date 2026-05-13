using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LineCom.Api.Modules.Account.DTOs;
using LineCom.Api.Modules.Account.Services;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Account;

public sealed class AccountProfileEndpointTests
{
    [Fact]
    public async Task GetProfile_AfterLogin_ReturnsCurrentProfile()
    {
        var user = TestUser();
        await using var factory = CreateFactory(new ReturningAccountProfileService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/account/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AccountProfileDto>(response);
        Assert.Equal(user.Id, body.User.Id);
        Assert.Equal("Ivan Petrov", body.User.Name);
        Assert.NotNull(body.Organization);
        Assert.Equal("ООО Сеть", body.Organization.Name);
        Assert.Equal("sales@example.com", body.Organization.Email);
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/account/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task PutProfile_ReturnsUpdatedCurrentUser()
    {
        var user = TestUser();
        await using var factory = CreateFactory(new ReturningAccountProfileService(user));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile")
        {
            Content = JsonContent.Create(
                new UpdateAccountProfileRequest("Ivan Petrov", "ivan@example.com", "+79000000000"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<CurrentUserDto>(response);
        Assert.Equal(user.Id, body.Id);
        Assert.Equal("Ivan Petrov", body.Name);
        Assert.Equal("ivan@example.com", body.Email);
    }

    [Fact]
    public async Task PutProfile_WithoutCsrfToken_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.PutAsJsonAsync(
            "/api/account/profile",
            new UpdateAccountProfileRequest("Ivan Petrov", "ivan@example.com", "+79000000000"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PutPassword_WithCsrfToken_ReturnsNoContent()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
        {
            Content = JsonContent.Create(new ChangeAccountPasswordRequest("old-password", "new-password"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutPassword_WithoutCsrfToken_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.PutAsJsonAsync(
            "/api/account/password",
            new ChangeAccountPasswordRequest("old-password", "new-password"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PutPassword_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
        {
            Content = JsonContent.Create(new ChangeAccountPasswordRequest("old-password", "new-password"))
        };
        request.Headers.Add("X-CSRF-Token", "csrf-token");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task PutOrganization_ReturnsUpsertedOrganization()
    {
        await using var factory = CreateFactory(new ReturningAccountProfileService(TestUser()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/organization")
        {
            Content = JsonContent.Create(new UpsertAccountOrganizationRequest(
                "ООО Сеть",
                "7700000000",
                "Ivan Petrov",
                "+79000000000",
                "sales@example.com",
                "Main organization"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AccountOrganizationDto>(response);
        Assert.Equal("ООО Сеть", body.Name);
        Assert.Equal("7700000000", body.Inn);
        Assert.Equal("Ivan Petrov", body.ContactPerson);
        Assert.Equal("+79000000000", body.Phone);
        Assert.Equal("sales@example.com", body.Email);
        Assert.Equal("Main organization", body.Comment);
    }

    [Fact]
    public async Task PutProfile_WhenCurrentUserInactive_ReturnsUserInactiveError()
    {
        await using var factory = CreateFactory(new ThrowingAccountProfileService(AuthErrors.UserInactive()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateAccountProfileRequest("Ivan Petrov", "ivan@example.com", null))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.user_inactive", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(IAccountProfileService accountProfileService)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAccountProfileService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(accountProfileService);
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser()));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository());
                });
            });
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

    private static CurrentUserDto TestUser()
    {
        return new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");
    }

    private sealed class ReturningAccountProfileService : IAccountProfileService
    {
        private readonly CurrentUserDto _user;
        private readonly AccountOrganizationDto _organization = new(
            "ООО Сеть",
            "7700000000",
            "Ivan Petrov",
            "+79000000000",
            "sales@example.com",
            "Main organization");

        public ReturningAccountProfileService(CurrentUserDto user)
        {
            _user = user;
        }

        public Task<AccountProfileDto> GetProfileAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AccountProfileDto(_user, _organization));
        }

        public Task<CurrentUserDto> UpdateProfileAsync(
            HttpContext httpContext,
            UpdateAccountProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }

        public Task<AccountOrganizationDto> UpsertOrganizationAsync(
            HttpContext httpContext,
            UpsertAccountOrganizationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_organization);
        }

        public Task ChangePasswordAsync(
            HttpContext httpContext,
            ChangeAccountPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAccountProfileService : IAccountProfileService
    {
        private readonly Exception _exception;

        public ThrowingAccountProfileService(Exception exception)
        {
            _exception = exception;
        }

        public Task<AccountProfileDto> GetProfileAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<CurrentUserDto> UpdateProfileAsync(
            HttpContext httpContext,
            UpdateAccountProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<AccountOrganizationDto> UpsertOrganizationAsync(
            HttpContext httpContext,
            UpsertAccountOrganizationRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task ChangePasswordAsync(
            HttpContext httpContext,
            ChangeAccountPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
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
