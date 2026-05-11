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

public sealed class AdminCatalogProductImagesEndpointTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ImageId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherImageId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid StoredFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetProductImages_WithoutAuth_ReturnsUnauthorizedError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogImageService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/admin/catalog/products/{ProductId}/images");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.unauthorized", body.Code);
    }

    [Fact]
    public async Task GetProductImages_AsCustomer_ReturnsForbiddenError()
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogImageService(), "customer");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/products/{ProductId}/images");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task GetProductImages_AsSeller_ReturnsImages()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/admin/catalog/products/{ProductId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminProductImagesResponse>(response);
        var image = Assert.Single(body.Items);
        Assert.Equal(ImageId, image.Id);
        Assert.Equal(StoredFileId, image.StoredFileId);
        Assert.Equal(ProductId, imageService.LastProductId);
    }

    [Fact]
    public async Task UploadProductImages_WithCsrfToken_ReturnsImages()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("jpeg-bytes"u8.ToArray())
        {
            Headers =
            {
                ContentType = new("image/jpeg")
            }
        }, "files", "cable.jpg");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/catalog/products/{ProductId}/images")
        {
            Content = content
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AdminProductImagesResponse>(response);
        Assert.Single(body.Items);
        Assert.Equal(ProductId, imageService.LastProductId);
        Assert.NotNull(imageService.LastUploadedFiles);
        var uploadedFile = Assert.Single(imageService.LastUploadedFiles);
        Assert.Equal("cable.jpg", uploadedFile.FileName);
        Assert.Equal("image/jpeg", uploadedFile.ContentType);
    }

    [Theory]
    [InlineData("POST", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images")]
    [InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/order")]
    [InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    [InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/main")]
    [InlineData("DELETE", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
    {
        await using var factory = CreateFactory(new ReturningAdminCatalogImageService(), "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        using var response = await client.SendAsync(CreateMutationRequest(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await ReadJsonAsync<ApiErrorResponse>(response);
        Assert.Equal("auth.forbidden", body.Code);
    }

    [Fact]
    public async Task PutProductImageOrder_WithCsrfToken_PassesCommand()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/catalog/products/{ProductId}/images/order")
        {
            Content = JsonContent.Create(new UpdateAdminProductImageOrderCommand([OtherImageId, ImageId]))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProductId, imageService.LastProductId);
        Assert.NotNull(imageService.LastOrderCommand);
        Assert.Equal([OtherImageId, ImageId], imageService.LastOrderCommand.ImageIds);
    }

    [Fact]
    public async Task PutProductImage_WithCsrfToken_PassesCommand()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/catalog/products/{ProductId}/images/{ImageId}")
        {
            Content = JsonContent.Create(new UpdateAdminProductImageCommand("Updated alt", "Updated title"))
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProductId, imageService.LastProductId);
        Assert.Equal(ImageId, imageService.LastImageId);
        Assert.NotNull(imageService.LastUpdateCommand);
        Assert.Equal("Updated alt", imageService.LastUpdateCommand.Alt);
        Assert.Equal("Updated title", imageService.LastUpdateCommand.Title);
    }

    [Fact]
    public async Task PutMainImage_WithCsrfToken_CallsService()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/catalog/products/{ProductId}/images/{ImageId}/main");
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProductId, imageService.LastProductId);
        Assert.Equal(ImageId, imageService.LastImageId);
        Assert.True(imageService.SetMainWasCalled);
    }

    [Fact]
    public async Task DeleteProductImage_WithCsrfToken_ReturnsNoContent()
    {
        var imageService = new ReturningAdminCatalogImageService();
        await using var factory = CreateFactory(imageService, "seller");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var csrfToken = await LoginAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/catalog/products/{ProductId}/images/{ImageId}");
        request.Headers.Add("X-CSRF-Token", csrfToken);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ProductId, imageService.LastProductId);
        Assert.Equal(ImageId, imageService.LastImageId);
        Assert.True(imageService.DeleteWasCalled);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminCatalogImageService imageService,
        string role)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAdminCatalogImageService>();
                    services.RemoveAll<ICustomerLoginService>();
                    services.RemoveAll<IUserLoginRepository>();

                    services.AddSingleton(imageService);
                    services.AddSingleton<ICustomerLoginService>(new ReturningCustomerLoginService(TestUser(role)));
                    services.AddSingleton<IUserLoginRepository>(new TestUserLoginRepository(role));
                });
            });
    }

    private static HttpRequestMessage CreateMutationRequest(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST")
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent("jpeg-bytes"u8.ToArray())
            {
                Headers =
                {
                    ContentType = new("image/jpeg")
                }
            }, "files", "cable.jpg");
            request.Content = content;
        }
        else if (method is not "DELETE")
        {
            request.Content = path.EndsWith("/order", StringComparison.Ordinal)
                ? JsonContent.Create(new UpdateAdminProductImageOrderCommand([ImageId]))
                : JsonContent.Create(new UpdateAdminProductImageCommand("Alt", "Title"));
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

    private sealed class ReturningAdminCatalogImageService : IAdminCatalogImageService
    {
        public Guid? LastProductId { get; private set; }
        public Guid? LastImageId { get; private set; }
        public IReadOnlyList<IFormFile>? LastUploadedFiles { get; private set; }
        public UpdateAdminProductImageOrderCommand? LastOrderCommand { get; private set; }
        public UpdateAdminProductImageCommand? LastUpdateCommand { get; private set; }
        public bool SetMainWasCalled { get; private set; }
        public bool DeleteWasCalled { get; private set; }

        public Task<AdminProductImagesResponse> GetProductImagesAsync(
            HttpContext httpContext,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            return Task.FromResult(Response());
        }

        public Task<AdminProductImagesResponse> UploadProductImagesAsync(
            HttpContext httpContext,
            Guid productId,
            IReadOnlyList<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            LastUploadedFiles = files;
            return Task.FromResult(Response());
        }

        public Task<AdminProductImageDto> UpdateProductImageAsync(
            HttpContext httpContext,
            Guid productId,
            Guid imageId,
            UpdateAdminProductImageCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            LastImageId = imageId;
            LastUpdateCommand = command;
            return Task.FromResult(Image(isMain: false));
        }

        public Task<AdminProductImagesResponse> UpdateProductImageOrderAsync(
            HttpContext httpContext,
            Guid productId,
            UpdateAdminProductImageOrderCommand command,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            LastOrderCommand = command;
            return Task.FromResult(Response());
        }

        public Task<AdminProductImageDto> SetMainProductImageAsync(
            HttpContext httpContext,
            Guid productId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            LastImageId = imageId;
            SetMainWasCalled = true;
            return Task.FromResult(Image(isMain: true));
        }

        public Task DeleteProductImageAsync(
            HttpContext httpContext,
            Guid productId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            RequireStaff(httpContext);
            LastProductId = productId;
            LastImageId = imageId;
            DeleteWasCalled = true;
            return Task.CompletedTask;
        }

        private static AdminProductImagesResponse Response()
        {
            return new AdminProductImagesResponse([Image(isMain: true)]);
        }

        private static AdminProductImageDto Image(bool isMain)
        {
            return new AdminProductImageDto(
                ImageId,
                StoredFileId,
                "/storage/products/admin/cable.jpg",
                "cable.jpg",
                "image/jpeg",
                10,
                "checksum",
                "Cable image",
                "Cable title",
                1,
                isMain,
                DateTimeOffset.Parse("2026-05-11T00:00:00Z"));
        }

        private static void RequireStaff(HttpContext httpContext)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }
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
