using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageServiceTests
{
    [Fact]
    public async Task GetSectionsAsync_RequiresStaffRole()
    {
        var service = new AdminHomepageService(new RejectingStaffGuard(), new StubQuery(), new StubRepository());

        await Assert.ThrowsAsync<ApiException>(() => service.GetSectionsAsync(new DefaultHttpContext()));
    }

    [Fact]
    public async Task CreateItemAsync_RejectsMissingAndDoubleTargets()
    {
        var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), new StubRepository());

        await Assert.ThrowsAsync<ApiException>(() => service.CreateItemAsync(
            new DefaultHttpContext(),
            Guid.NewGuid(),
            new CreateAdminHomepageSectionItemCommand(null, null, 10, true)));

        await Assert.ThrowsAsync<ApiException>(() => service.CreateItemAsync(
            new DefaultHttpContext(),
            Guid.NewGuid(),
            new CreateAdminHomepageSectionItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10, true)));
    }

    [Fact]
    public async Task UpdateItemOrderAsync_RejectsEmptyOrder()
    {
        var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), new StubRepository());

        await Assert.ThrowsAsync<ApiException>(() => service.UpdateItemOrderAsync(
            new DefaultHttpContext(),
            Guid.NewGuid(),
            new UpdateAdminHomepageSectionItemOrderCommand([])));
    }

    [Fact]
    public async Task UpdateItemOrderAsync_RejectsDuplicateOrderIds()
    {
        var itemId = Guid.NewGuid();
        var repository = new StubRepository();
        var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.UpdateItemOrderAsync(
            new DefaultHttpContext(),
            Guid.NewGuid(),
            new UpdateAdminHomepageSectionItemOrderCommand([itemId, itemId])));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
        Assert.Null(repository.LastItemOrderIds);
    }

    [Fact]
    public async Task UpdateSectionAsync_AllowsNullTitleForPartialUpdate()
    {
        var repository = new StubRepository
        {
            UpdatedSection = new AdminHomepageSectionDto(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "featured_products",
                "Главные товары",
                "product",
                6,
                10,
                false,
                [])
        };
        var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), repository);

        var response = await service.UpdateSectionAsync(
            new DefaultHttpContext(),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new UpdateAdminHomepageSectionCommand(null, null, null, false));

        Assert.False(response.IsActive);
        Assert.NotNull(repository.LastUpdateSectionCommand);
        Assert.Null(repository.LastUpdateSectionCommand.Title);
        Assert.False(repository.LastUpdateSectionCommand.IsActive);
    }

    private sealed class RejectingStaffGuard : IAdminCatalogStaffGuard
    {
        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            throw AuthErrors.Forbidden();
        }
    }

    private sealed class AllowingStaffGuard : IAdminCatalogStaffGuard
    {
        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CurrentUserDto(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Staff User",
                "staff@example.com",
                null,
                "seller"));
        }
    }

    private sealed class StubQuery : IAdminHomepageQuery
    {
        public Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AdminHomepageSectionsResponse([]));
        }
    }

    private sealed class StubRepository : IAdminHomepageRepository
    {
        public UpdateAdminHomepageSectionCommand? LastUpdateSectionCommand { get; private set; }
        public IReadOnlyList<Guid>? LastItemOrderIds { get; private set; }
        public AdminHomepageSectionDto? UpdatedSection { get; init; }

        public Task<bool> SectionExistsAsync(Guid sectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<AdminHomepageSectionDto?> UpdateSectionAsync(
            Guid sectionId,
            UpdateAdminHomepageSectionCommand command,
            CancellationToken cancellationToken = default)
        {
            LastUpdateSectionCommand = command;
            return Task.FromResult(UpdatedSection);
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
            LastItemOrderIds = itemIds;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteItemAsync(Guid sectionId, Guid itemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
