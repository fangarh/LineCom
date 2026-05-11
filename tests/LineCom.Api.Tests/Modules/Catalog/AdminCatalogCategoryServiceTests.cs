using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogCategoryServiceTests
{
    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetCategoriesAsync_AllowsSellerAndAdmin(string role)
    {
        var repository = new CapturingAdminCatalogCategoryRepository();
        var service = CreateService(role, repository);

        await service.GetCategoriesAsync(
            new DefaultHttpContext(),
            new AdminCategoryListQuery(1, 20, null, null, null),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
    }

    [Fact]
    public async Task GetCategoriesAsync_RejectsCustomer()
    {
        var service = CreateService("customer", new CapturingAdminCatalogCategoryRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetCategoriesAsync(
                new DefaultHttpContext(),
                new AdminCategoryListQuery(1, 20, null, null, null),
                CancellationToken.None));

        Assert.Equal("auth.forbidden", exception.Code);
    }

    [Fact]
    public async Task CreateCategoryAsync_TrimsRequiredStrings()
    {
        var repository = new CapturingAdminCatalogCategoryRepository();
        var service = CreateService("seller", repository);

        var response = await service.CreateCategoryAsync(
            new DefaultHttpContext(),
            new UpsertAdminCategoryCommand(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                " Cable trays ",
                " cable-trays ",
                " Description ",
                " SEO title ",
                " SEO description ",
                " H1 ",
                15,
                true,
                false),
            CancellationToken.None);

        Assert.NotNull(repository.LastUpsert);
        var upsert = repository.LastUpsert;
        Assert.Equal("Cable trays", upsert.Name);
        Assert.Equal("cable-trays", upsert.Slug);
        Assert.Equal("Description", upsert.Description);
        Assert.Equal("SEO title", upsert.SeoTitle);
        Assert.Equal("SEO description", upsert.SeoDescription);
        Assert.Equal("H1", upsert.H1);
        Assert.Equal(15, upsert.SortOrder);
        Assert.True(upsert.IsActive);
        Assert.False(upsert.IsVisibleInMenu);
        Assert.Equal("Cable trays", response.Name);
        Assert.Equal("cable-trays", response.Slug);
    }

    [Theory]
    [InlineData("   ", "category")]
    [InlineData("Category", "   ")]
    public async Task CreateCategoryAsync_RejectsBlankNameOrSlug(string name, string slug)
    {
        var service = CreateService("seller", new CapturingAdminCatalogCategoryRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateCategoryAsync(
                new DefaultHttpContext(),
                new UpsertAdminCategoryCommand(null, name, slug, null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithUsage_ThrowsEntityInUse()
    {
        var repository = new CapturingAdminCatalogCategoryRepository { UsageCount = 2 };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteCategoryAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.Equal(
            "\u041a\u0430\u0442\u0435\u0433\u043e\u0440\u0438\u044f \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0438 \u043d\u0435 \u043c\u043e\u0436\u0435\u0442 \u0431\u044b\u0442\u044c \u0443\u0434\u0430\u043b\u0435\u043d\u0430.",
            exception.Message);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task GetCategoryAsync_MissingDetail_ThrowsCategoryNotFound()
    {
        var repository = new CapturingAdminCatalogCategoryRepository { Detail = null };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetCategoryAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.category_not_found", exception.Code);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateSlug_ThrowsSlugAlreadyExists()
    {
        var repository = new CapturingAdminCatalogCategoryRepository
        {
            CreateException = new AdminCategorySlugAlreadyExistsException()
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateCategoryAsync(
                new DefaultHttpContext(),
                new UpsertAdminCategoryCommand(null, "Category", "category", null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.slug_already_exists", exception.Code);
    }

    [Fact]
    public async Task UpdateCategoryAsync_InvalidParent_ThrowsInvalidRequest()
    {
        var repository = new CapturingAdminCatalogCategoryRepository
        {
            UpdateException = new InvalidAdminCategoryParentException()
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateCategoryAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new UpsertAdminCategoryCommand(null, "Category", "category", null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task MoveCategoryAsync_InvalidParent_ThrowsInvalidRequest()
    {
        var repository = new CapturingAdminCatalogCategoryRepository
        {
            MoveException = new InvalidAdminCategoryParentException()
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.MoveCategoryAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new MoveAdminCategoryCommand(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task DeleteCategoryAsync_RaceWithUsage_ThrowsEntityInUse()
    {
        var repository = new CapturingAdminCatalogCategoryRepository
        {
            DeleteException = new AdminCategoryInUseException()
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteCategoryAsync(
                new DefaultHttpContext(),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.Equal(
            "\u041a\u0430\u0442\u0435\u0433\u043e\u0440\u0438\u044f \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0438 \u043d\u0435 \u043c\u043e\u0436\u0435\u0442 \u0431\u044b\u0442\u044c \u0443\u0434\u0430\u043b\u0435\u043d\u0430.",
            exception.Message);
    }

    private static AdminCatalogCategoryService CreateService(
        string role,
        CapturingAdminCatalogCategoryRepository repository)
    {
        return new AdminCatalogCategoryService(
            new RoleAdminCatalogStaffGuard(role),
            repository);
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

    private sealed class CapturingAdminCatalogCategoryRepository : IAdminCatalogCategoryRepository
    {
        public AdminCategoryReadListQuery? LastListQuery { get; private set; }
        public AdminCategoryUpsert? LastUpsert { get; private set; }
        public int UsageCount { get; init; }
        public bool DeleteCalled { get; private set; }
        public AdminCategoryRecord? Detail { get; init; } = CategoryRecord();
        public Exception? CreateException { get; init; }
        public Exception? UpdateException { get; init; }
        public Exception? MoveException { get; init; }
        public Exception? DeleteException { get; init; }

        public Task<AdminCategoryListRecordResponse> GetCategoriesAsync(
            AdminCategoryReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            return Task.FromResult(new AdminCategoryListRecordResponse(
                new[] { CategoryRecord() },
                TotalItems: 1));
        }

        public Task<AdminCategoryRecord?> GetCategoryAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Detail);
        }

        public Task<AdminCategoryRecord> CreateCategoryAsync(
            AdminCategoryUpsert command,
            CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            LastUpsert = command;

            return Task.FromResult(CategoryRecord(
                name: command.Name,
                slug: command.Slug,
                description: command.Description,
                seoTitle: command.SeoTitle,
                seoDescription: command.SeoDescription,
                h1: command.H1,
                sortOrder: command.SortOrder,
                isActive: command.IsActive,
                isVisibleInMenu: command.IsVisibleInMenu));
        }

        public Task<AdminCategoryRecord?> UpdateCategoryAsync(
            Guid id,
            AdminCategoryUpsert command,
            CancellationToken cancellationToken = default)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            LastUpsert = command;
            return Task.FromResult<AdminCategoryRecord?>(CategoryRecord());
        }

        public Task<AdminCategoryRecord?> MoveCategoryAsync(
            Guid id,
            Guid? parentId,
            CancellationToken cancellationToken = default)
        {
            if (MoveException is not null)
            {
                throw MoveException;
            }

            return Task.FromResult<AdminCategoryRecord?>(CategoryRecord(parentId: parentId));
        }

        public Task<AdminCategoryRecord?> SortCategoryAsync(
            Guid id,
            int sortOrder,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminCategoryRecord?>(CategoryRecord(sortOrder: sortOrder));
        }

        public Task<int> CountCategoryUsageAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsageCount);
        }

        public Task<bool> DeleteCategoryAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            DeleteCalled = true;
            return Task.FromResult(true);
        }

        private static AdminCategoryRecord CategoryRecord(
            Guid? parentId = null,
            string name = "Cable",
            string slug = "cable",
            string? description = "Description",
            string? seoTitle = "SEO title",
            string? seoDescription = "SEO description",
            string? h1 = "Cable H1",
            int sortOrder = 10,
            bool isActive = true,
            bool isVisibleInMenu = true)
        {
            return new AdminCategoryRecord(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                parentId,
                name,
                slug,
                description,
                seoTitle,
                seoDescription,
                h1,
                sortOrder,
                isActive,
                isVisibleInMenu,
                ProductsCount: 3,
                ChildrenCount: 1);
        }
    }
}
