using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
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

public sealed class AdminRequestsEndpointTests
{
    [Fact]
    public async Task GetRequests_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminRequestService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/admin/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetRequests_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminRequestService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/requests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetRequests_AsSeller_ReturnsFilteredRequests()
    {
        var requestService = new ReturningAdminRequestService();
        await using var factory = CreateFactory(requestService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync(
            "/api/admin/requests?page=2&pageSize=10&status=new&number=ZK26-0002&contact=ivan&organization=OOO%20Cable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminRequestListResponse>(response);
        Assert.Equal(2, body.Page);
        Assert.Equal(10, body.PageSize);
        var item = Assert.Single(body.Items);
        Assert.Equal("ZK26-0002", item.Number);
        Assert.Equal("new", item.Status.Code);
        Assert.NotNull(requestService.LastListQuery);
        Assert.Equal(2, requestService.LastListQuery.Page);
        Assert.Equal(10, requestService.LastListQuery.PageSize);
        Assert.Equal("new", requestService.LastListQuery.Status);
        Assert.Equal("ZK26-0002", requestService.LastListQuery.Number);
        Assert.Equal("ivan", requestService.LastListQuery.Contact);
        Assert.Equal("OOO Cable", requestService.LastListQuery.Organization);
    }

    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetRequest_AsStaff_ReturnsRequestDetail(string role)
    {
        await using var factory = CreateFactory(new ReturningAdminRequestService(), role);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync("/api/admin/requests/ZK26-0002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminRequestDetailDto>(response);
        Assert.Equal("ZK26-0002", body.Number);
        Assert.Equal("Ivan Petrov", body.Customer.Name);
        Assert.Equal("OOO Cable", body.Organization?.Name);
        Assert.Equal("created", Assert.Single(body.History).Event);
    }

    [Fact]
    public async Task PatchStatus_WithCsrfToken_ReturnsUpdatedRequestDetail()
    {
        var requestService = new ReturningAdminRequestService();
        await using var factory = CreateFactory(requestService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/admin/requests/ZK26-0002/status")
        {
            Content = JsonContent.Create(new UpdateAdminRequestStatusCommand("in_progress"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminRequestDetailDto>(response);
        Assert.Equal("ZK26-0002", body.Number);
        Assert.Equal("in_progress", body.Status.Code);
        Assert.Equal("ZK26-0002", requestService.LastStatusNumber);
        Assert.Equal("in_progress", requestService.LastStatusCommand?.Status);
    }

    [Fact]
    public async Task PatchStatus_WithoutCsrfToken_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminRequestService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.PatchAsJsonAsync(
            "/api/admin/requests/ZK26-0002/status",
            new UpdateAdminRequestStatusCommand("in_progress"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PutInternalComment_WithCsrfToken_ReturnsUpdatedRequestDetail()
    {
        var requestService = new ReturningAdminRequestService();
        await using var factory = CreateFactory(requestService, "admin");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/requests/ZK26-0002/internal-comment")
        {
            Content = JsonContent.Create(new UpdateAdminRequestInternalCommentCommand("Call before processing"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminRequestDetailDto>(response);
        Assert.Equal("ZK26-0002", body.Number);
        Assert.Equal("Call before processing", body.InternalComment);
        Assert.Equal("ZK26-0002", requestService.LastInternalCommentNumber);
        Assert.Equal("Call before processing", requestService.LastInternalCommentCommand?.InternalComment);
    }

    [Fact]
    public async Task PutInternalComment_WithoutCsrfToken_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminRequestService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.PutAsJsonAsync(
            "/api/admin/requests/ZK26-0002/internal-comment",
            new UpdateAdminRequestInternalCommentCommand("Call before processing"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminRequestService requestService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminRequestService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(requestService);
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
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

    private static CurrentUserDto TestUser(string role)
    {
        return new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            role);
    }

    private sealed class ReturningAdminRequestService : IAdminRequestService
    {
        public AdminRequestListQuery? LastListQuery { get; private set; }

        public string? LastStatusNumber { get; private set; }

        public UpdateAdminRequestStatusCommand? LastStatusCommand { get; private set; }

        public string? LastInternalCommentNumber { get; private set; }

        public UpdateAdminRequestInternalCommentCommand? LastInternalCommentCommand { get; private set; }

        public Task<AdminRequestListResponse> GetRequestsAsync(
            HttpContext httpContext,
            AdminRequestListQuery query,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastListQuery = query;

            return Task.FromResult(new AdminRequestListResponse(
                new[]
                {
                    new AdminRequestListItemDto(
                        "ZK26-0002",
                        new RequestStatusDto("new", "New"),
                        "cart",
                        2,
                        Customer(),
                        Organization(),
                        "Need delivery date",
                        null,
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero),
                        new DateTimeOffset(2026, 5, 8, 9, 0, 0, TimeSpan.Zero))
                },
                Page: 2,
                PageSize: 10,
                TotalItems: 21,
                TotalPages: 3));
        }

        public Task<AdminRequestDetailDto> GetRequestAsync(
            HttpContext httpContext,
            string number,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            return Task.FromResult(Detail(number, "new", null));
        }

        public Task<AdminRequestDetailDto> UpdateStatusAsync(
            HttpContext httpContext,
            string number,
            UpdateAdminRequestStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastStatusNumber = number;
            LastStatusCommand = command;

            return Task.FromResult(Detail(number, command.Status ?? "new", null));
        }

        public Task<AdminRequestDetailDto> UpdateInternalCommentAsync(
            HttpContext httpContext,
            string number,
            UpdateAdminRequestInternalCommentCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastInternalCommentNumber = number;
            LastInternalCommentCommand = command;

            return Task.FromResult(Detail(number, "new", command.InternalComment));
        }

        private static void RequireStaff(HttpContext httpContext)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }
        }

        private static AdminRequestDetailDto Detail(
            string number,
            string status,
            string? internalComment)
        {
            return new AdminRequestDetailDto(
                number,
                new RequestStatusDto(status, status == "new" ? "New" : "In progress"),
                "cart",
                Customer(),
                Organization(),
                "Need delivery date",
                internalComment,
                new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 8, 9, 0, 0, TimeSpan.Zero),
                new[]
                {
                    new CustomerRequestItemDto(
                        Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                        "Cable U/UTP Cat 5e",
                        "LC-UTP5E-CU-305",
                        new PublicCodeLabelDto("coil", "Coil"),
                        "305 m",
                        2,
                        "Replace with analogue if faster")
                },
                new[]
                {
                    new CustomerRequestHistoryDto(
                        "created",
                        "Request created.",
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero))
                });
        }

        private static RequestCustomerSnapshotDto Customer()
        {
            return new RequestCustomerSnapshotDto(
                "Ivan Petrov",
                "ivan@example.com",
                "+79000000000");
        }

        private static RequestOrganizationSnapshotDto Organization()
        {
            return new RequestOrganizationSnapshotDto(
                "OOO Cable",
                "7700000000",
                "Ivan Petrov");
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
