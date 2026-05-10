using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class CustomerRequestsEndpointTests
{
    [Fact]
    public async Task PostRequest_AfterLogin_ReturnsCreatedRequest()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/requests")
        {
            Content = JsonContent.Create(new CreateRequestCommand(
                "cart",
                "Need delivery date",
                new[]
                {
                    new CreateRequestItemCommand(
                        Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                        2,
                        "Replace with analogue if faster")
                }))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/account/requests/%D0%97%D0%9A26-0001", response.Headers.Location?.OriginalString);

        var body = await ReadJsonAsync<CustomerRequestDetailDto>(response);
        Assert.Equal("ЗК26-0001", body.Number);
        Assert.Equal("new", body.Status.Code);
        Assert.Equal("cart", body.Source);
        Assert.Equal(2, Assert.Single(body.Items).Quantity);
    }

    [Fact]
    public async Task PostRequest_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsJsonAsync(
            "/api/account/requests",
            new CreateRequestCommand(
                "cart",
                null,
                new[] { new CreateRequestItemCommand(Guid.NewGuid(), 1, null) }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task PostRequest_WithoutCsrfToken_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.PostAsJsonAsync(
            "/api/account/requests",
            new CreateRequestCommand(
                "cart",
                null,
                new[] { new CreateRequestItemCommand(Guid.NewGuid(), 1, null) }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PostRequest_WhenCurrentUserInactive_ReturnsUserInactiveError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerRequestService(AuthErrors.UserInactive()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/requests")
        {
            Content = JsonContent.Create(new CreateRequestCommand(
                "cart",
                null,
                new[] { new CreateRequestItemCommand(Guid.NewGuid(), 1, null) }))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.user_inactive", body.Code);
    }

    [Fact]
    public async Task GetRequests_AfterLogin_ReturnsCurrentUserRequests()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/account/requests?page=2&pageSize=10&status=new");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<CustomerRequestListResponse>(response);
        Assert.Equal(2, body.Page);
        Assert.Equal(10, body.PageSize);
        Assert.Equal(21, body.TotalItems);
        Assert.Equal(3, body.TotalPages);
        var item = Assert.Single(body.Items);
        Assert.Equal("Р—Рљ26-0002", item.Number);
        Assert.Equal("new", item.Status.Code);
        Assert.Equal(2, item.ItemsCount);
    }

    [Fact]
    public async Task GetRequests_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/account/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetRequest_AfterLogin_ReturnsCurrentUserRequestDetail()
    {
        await using var factory = CreateFactory(new ReturningCustomerRequestService());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/account/requests/Р—Рљ26-0002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<CustomerRequestDetailDto>(response);
        Assert.Equal("Р—Рљ26-0002", body.Number);
        Assert.Equal("Ivan Petrov", body.Customer?.Name);
        Assert.Equal("OOO Cable", body.Organization?.Name);
        Assert.Equal("created", Assert.Single(body.History!).Event);
    }

    [Fact]
    public async Task GetRequest_WhenServiceReturnsNotFound_ReturnsNotFoundError()
    {
        await using var factory = CreateFactory(new ThrowingCustomerRequestService(RequestErrors.NotFound()));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/account/requests/Р—Рљ26-4040");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("request.not_found", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(ICustomerRequestService requestService)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICustomerRequestService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(requestService);
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

    private sealed class ReturningCustomerRequestService : ICustomerRequestService
    {
        public Task<CustomerRequestDetailDto> CreateRequestAsync(
            HttpContext httpContext,
            CreateRequestCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerRequestDetailDto(
                "ЗК26-0001",
                new RequestStatusDto("new", "Новая"),
                "cart",
                "Need delivery date",
                new DateTimeOffset(2026, 5, 7, 10, 15, 30, TimeSpan.Zero),
                new[]
                {
                    new CustomerRequestItemDto(
                        Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                        "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                        "LC-UTP5E-CU-305",
                        new LineCom.Api.Modules.Catalog.DTOs.PublicCodeLabelDto("coil", "бухта"),
                        "305 м",
                        2,
                        "Replace with analogue if faster")
                }));
        }

        public Task<CustomerRequestListResponse> GetRequestsAsync(
            HttpContext httpContext,
            CustomerRequestListQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerRequestListResponse(
                new[]
                {
                    new CustomerRequestListItemDto(
                        "Р—Рљ26-0002",
                        new RequestStatusDto("new", "РќРѕРІР°СЏ"),
                        "cart",
                        2,
                        "Need delivery date",
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero))
                },
                Page: 2,
                PageSize: 10,
                TotalItems: 21,
                TotalPages: 3));
        }

        public Task<CustomerRequestDetailDto> GetRequestAsync(
            HttpContext httpContext,
            string number,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CustomerRequestDetailDto(
                "Р—Рљ26-0002",
                new RequestStatusDto("new", "РќРѕРІР°СЏ"),
                "cart",
                "Need delivery date",
                new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero),
                new[]
                {
                    new CustomerRequestItemDto(
                        Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                        "РљР°Р±РµР»СЊ U/UTP Cat 5e 4 РїР°СЂС‹ CU 305 Рј",
                        "LC-UTP5E-CU-305",
                        new LineCom.Api.Modules.Catalog.DTOs.PublicCodeLabelDto("coil", "Р±СѓС…С‚Р°"),
                        "305 Рј",
                        2,
                        "Replace with analogue if faster")
                },
                new RequestCustomerSnapshotDto(
                    "Ivan Petrov",
                    "ivan@example.com",
                    "+79000000000"),
                new RequestOrganizationSnapshotDto(
                    "OOO Cable",
                    "7700000000",
                    "Ivan Petrov"),
                new[]
                {
                    new CustomerRequestHistoryDto(
                        "created",
                        "Р—Р°СЏРІРєР° СЃРѕР·РґР°РЅР°.",
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero))
                }));
        }
    }

    private sealed class ThrowingCustomerRequestService : ICustomerRequestService
    {
        private readonly Exception _exception;

        public ThrowingCustomerRequestService(Exception exception)
        {
            _exception = exception;
        }

        public Task<CustomerRequestDetailDto> CreateRequestAsync(
            HttpContext httpContext,
            CreateRequestCommand command,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<CustomerRequestListResponse> GetRequestsAsync(
            HttpContext httpContext,
            CustomerRequestListQuery query,
            CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public Task<CustomerRequestDetailDto> GetRequestAsync(
            HttpContext httpContext,
            string number,
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
