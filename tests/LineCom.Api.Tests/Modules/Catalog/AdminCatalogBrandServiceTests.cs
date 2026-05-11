using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogBrandServiceTests
{
    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetBrandsAsync_AllowsSellerAndAdmin(string role)
    {
        var repository = new CapturingAdminCatalogBrandRepository();
        var service = CreateService(role, repository);

        await service.GetBrandsAsync(
            new DefaultHttpContext(),
            new AdminBrandListQuery(1, 20, null, null),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
    }

    [Fact]
    public async Task GetBrandsAsync_RejectsCustomer()
    {
        var service = CreateService("customer", new CapturingAdminCatalogBrandRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetBrandsAsync(
                new DefaultHttpContext(),
                new AdminBrandListQuery(1, 20, null, null),
                CancellationToken.None));

        Assert.Equal("auth.forbidden", exception.Code);
    }

    [Theory]
    [InlineData("   ", "brand")]
    [InlineData("Brand", "   ")]
    public async Task CreateBrandAsync_RejectsBlankNameOrSlug(string name, string slug)
    {
        var service = CreateService("seller", new CapturingAdminCatalogBrandRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateBrandAsync(
                new DefaultHttpContext(),
                new UpsertAdminBrandCommand(name, slug, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task GetBrandAsync_MissingDetail_ThrowsBrandNotFound()
    {
        var repository = new CapturingAdminCatalogBrandRepository { Detail = null };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetBrandAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.brand_not_found", exception.Code);
    }

    [Fact]
    public async Task DeleteBrandAsync_WithProducts_ThrowsEntityInUse()
    {
        var repository = new CapturingAdminCatalogBrandRepository
        {
            Detail = BrandRecord(productsCount: 2)
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteBrandAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.Equal("\u0411\u0440\u0435\u043d\u0434 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0442\u043e\u0432\u0430\u0440\u0430\u043c\u0438.", exception.Message);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task DeleteBrandAsync_WhenUsageAppearsDuringDelete_ThrowsEntityInUse()
    {
        var repository = new CapturingAdminCatalogBrandRepository
        {
            Detail = BrandRecord(productsCount: 0),
            DetailAfterDelete = BrandRecord(productsCount: 1),
            DeleteResult = false
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteBrandAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
    }

    [Fact]
    public async Task QuickCreateBrandAsync_RequiresOnlyName()
    {
        var repository = new CapturingAdminCatalogBrandRepository();
        var service = CreateService("seller", repository);

        var response = await service.QuickCreateBrandAsync(
            new DefaultHttpContext(),
            new QuickCreateAdminBrandCommand(" Cablex "),
            CancellationToken.None);

        Assert.NotNull(repository.LastQuickCreate);
        Assert.Equal("Cablex", repository.LastQuickCreate.Name);
        Assert.False(string.IsNullOrWhiteSpace(repository.LastQuickCreate.Slug));
        Assert.Equal("Cablex", response.Name);
    }

    private static AdminCatalogBrandService CreateService(
        string role,
        CapturingAdminCatalogBrandRepository repository)
    {
        return new AdminCatalogBrandService(
            new RoleAdminCatalogStaffGuard(role),
            repository);
    }

    private static AdminBrandRecord BrandRecord(
        string name = "Cablex",
        string slug = "cablex",
        int productsCount = 0)
    {
        return new AdminBrandRecord(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            name,
            slug,
            "Description",
            "SEO title",
            "SEO description",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            IsActive: true,
            productsCount);
    }

    private sealed class RoleAdminCatalogStaffGuard : IAdminCatalogStaffGuard
    {
        private readonly string _role;

        public RoleAdminCatalogStaffGuard(string role)
        {
            _role = role;
        }

        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            if (_role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }

            return Task.FromResult(new CurrentUserDto(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Staff User",
                "staff@example.com",
                null,
                _role));
        }
    }

    private sealed class CapturingAdminCatalogBrandRepository : IAdminCatalogBrandRepository
    {
        public AdminBrandReadListQuery? LastListQuery { get; private set; }
        public AdminBrandUpsert? LastUpsert { get; private set; }
        public AdminBrandQuickCreate? LastQuickCreate { get; private set; }
        public bool DeleteCalled { get; private set; }
        public AdminBrandRecord? Detail { get; init; } = BrandRecord();
        public AdminBrandRecord? DetailAfterDelete { get; init; }
        public bool DeleteResult { get; init; } = true;
        private bool _deleteAttempted;

        public Task<AdminBrandListRecordResponse> GetBrandsAsync(
            AdminBrandReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            return Task.FromResult(new AdminBrandListRecordResponse(
                new[] { BrandRecord() },
                TotalItems: 1));
        }

        public Task<AdminBrandRecord?> GetBrandAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (_deleteAttempted && DetailAfterDelete is not null)
            {
                return Task.FromResult<AdminBrandRecord?>(DetailAfterDelete);
            }

            return Task.FromResult(Detail);
        }

        public Task<AdminBrandRecord> CreateBrandAsync(
            AdminBrandUpsert command,
            CancellationToken cancellationToken = default)
        {
            LastUpsert = command;
            return Task.FromResult(BrandRecord(command.Name, command.Slug));
        }

        public Task<AdminBrandRecord?> UpdateBrandAsync(
            Guid id,
            AdminBrandUpsert command,
            CancellationToken cancellationToken = default)
        {
            LastUpsert = command;
            return Task.FromResult<AdminBrandRecord?>(BrandRecord(command.Name, command.Slug));
        }

        public Task<AdminBrandRecord> QuickCreateBrandAsync(
            AdminBrandQuickCreate command,
            CancellationToken cancellationToken = default)
        {
            LastQuickCreate = command;
            return Task.FromResult(BrandRecord(command.Name, command.Slug));
        }

        public Task<bool> DeleteBrandAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            _deleteAttempted = true;
            return Task.FromResult(DeleteResult);
        }
    }
}
