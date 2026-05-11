using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogAttributeServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AttributeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OptionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task GetAttributesAsync_LoadsAttributesAndOptions()
    {
        var repository = new CapturingAdminCatalogAttributeRepository();
        var service = CreateService("seller", repository);

        var response = await service.GetAttributesAsync(
            new DefaultHttpContext(),
            CategoryId,
            CancellationToken.None);

        var attribute = Assert.Single(response.Items);
        Assert.Equal(AttributeId, attribute.Id);
        Assert.Equal("Voltage", attribute.Name);
        Assert.Equal("voltage", attribute.Code);
        Assert.Equal("select", attribute.Type);
        Assert.Equal(2, attribute.ProductValuesCount);

        var option = Assert.Single(attribute.Options);
        Assert.Equal(OptionId, option.Id);
        Assert.Equal("220 V", option.Value);
        Assert.Equal(1, option.ProductValuesCount);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("number")]
    [InlineData("select")]
    [InlineData("boolean")]
    public async Task CreateAttributeAsync_AllowsSupportedTypes(string type)
    {
        var repository = new CapturingAdminCatalogAttributeRepository();
        var service = CreateService("admin", repository);

        await service.CreateAttributeAsync(
            new DefaultHttpContext(),
            CategoryId,
            new UpsertAdminCategoryAttributeCommand(
                " Voltage ",
                " voltage ",
                type,
                " V ",
                true,
                true,
                false,
                true,
                false,
                false,
                10,
                true),
            CancellationToken.None);

        Assert.NotNull(repository.LastAttributeUpsert);
        Assert.Equal("Voltage", repository.LastAttributeUpsert.Name);
        Assert.Equal("voltage", repository.LastAttributeUpsert.Code);
        Assert.Equal(type, repository.LastAttributeUpsert.Type);
        Assert.Equal("V", repository.LastAttributeUpsert.Unit);
    }

    [Theory]
    [InlineData("   ", "code", "text")]
    [InlineData("Name", "   ", "text")]
    [InlineData("Name", "code", "unsupported")]
    public async Task CreateAttributeAsync_RejectsBlankRequiredFieldsAndUnsupportedTypes(
        string name,
        string code,
        string type)
    {
        var service = CreateService("seller", new CapturingAdminCatalogAttributeRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateAttributeAsync(
                new DefaultHttpContext(),
                CategoryId,
                new UpsertAdminCategoryAttributeCommand(name, code, type, null, null, null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task UpdateAttributeAsync_CannotChangeTypeWhenValuesExist()
    {
        var repository = new CapturingAdminCatalogAttributeRepository
        {
            ExistingAttribute = AttributeRecord(type: "text", productValuesCount: 3)
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateAttributeAsync(
                new DefaultHttpContext(),
                CategoryId,
                AttributeId,
                new UpsertAdminCategoryAttributeCommand("Voltage", "voltage", "number", null, null, null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.False(repository.UpdateAttributeCalled);
    }

    [Fact]
    public async Task UpdateAttributeAsync_CannotChangeSelectTypeWhenOptionsExist()
    {
        var repository = new CapturingAdminCatalogAttributeRepository
        {
            ExistingAttribute = AttributeRecord(type: "select", productValuesCount: 0)
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateAttributeAsync(
                new DefaultHttpContext(),
                CategoryId,
                AttributeId,
                new UpsertAdminCategoryAttributeCommand("Voltage", "voltage", "text", null, null, null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
        Assert.False(repository.UpdateAttributeCalled);
    }

    [Fact]
    public async Task DeleteAttributeAsync_CannotDeleteWhenValuesExist()
    {
        var repository = new CapturingAdminCatalogAttributeRepository
        {
            ExistingAttribute = AttributeRecord(productValuesCount: 2)
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteAttributeAsync(
                new DefaultHttpContext(),
                CategoryId,
                AttributeId,
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.False(repository.DeleteAttributeCalled);
    }

    [Fact]
    public async Task DeleteOptionAsync_CannotDeleteWhenProductValuesUseIt()
    {
        var repository = new CapturingAdminCatalogAttributeRepository
        {
            ExistingOption = OptionRecord(productValuesCount: 1)
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteOptionAsync(
                new DefaultHttpContext(),
                CategoryId,
                AttributeId,
                OptionId,
                CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.False(repository.DeleteOptionCalled);
    }

    [Fact]
    public async Task InheritFromParentAsync_ReturnsAddedAndSkippedCounts()
    {
        var repository = new CapturingAdminCatalogAttributeRepository
        {
            InheritResult = new AdminCategoryAttributeInheritanceResult(Added: 2, Skipped: 1)
        };
        var service = CreateService("admin", repository);

        var response = await service.InheritFromParentAsync(
            new DefaultHttpContext(),
            CategoryId,
            CancellationToken.None);

        Assert.Equal(2, response.Added);
        Assert.Equal(1, response.Skipped);
        Assert.True(repository.InheritCalled);
    }

    private static AdminCatalogAttributeService CreateService(
        string role,
        CapturingAdminCatalogAttributeRepository repository)
    {
        return new AdminCatalogAttributeService(
            new RoleAdminCatalogStaffGuard(role),
            repository);
    }

    private static AdminCategoryAttributeRecord AttributeRecord(
        string type = "select",
        int productValuesCount = 2)
    {
        return new AdminCategoryAttributeRecord(
            AttributeId,
            CategoryId,
            "Voltage",
            "voltage",
            type,
            "V",
            IsRequired: true,
            IsFilterable: true,
            IsComparable: false,
            IsVisibleInProduct: true,
            IsSeoImportant: false,
            IsUsedInGeneratedName: false,
            SortOrder: 10,
            IsActive: true,
            productValuesCount);
    }

    private static AdminAttributeOptionRecord OptionRecord(int productValuesCount = 1)
    {
        return new AdminAttributeOptionRecord(
            OptionId,
            AttributeId,
            "220 V",
            "220-v",
            "220 v",
            SortOrder: 10,
            IsActive: true,
            productValuesCount);
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
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                "Staff User",
                "staff@example.com",
                null,
                _role));
        }
    }

    private sealed class CapturingAdminCatalogAttributeRepository : IAdminCatalogAttributeRepository
    {
        public AdminCategoryAttributeUpsert? LastAttributeUpsert { get; private set; }
        public bool UpdateAttributeCalled { get; private set; }
        public bool DeleteAttributeCalled { get; private set; }
        public bool DeleteOptionCalled { get; private set; }
        public bool InheritCalled { get; private set; }
        public AdminCategoryAttributeRecord? ExistingAttribute { get; init; } = AttributeRecord();
        public AdminAttributeOptionRecord? ExistingOption { get; init; } = OptionRecord();
        public AdminCategoryAttributeInheritanceResult InheritResult { get; init; } = new(0, 0);

        public Task<IReadOnlyList<AdminCategoryAttributeRecord>> GetAttributesAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminCategoryAttributeRecord>>(new[] { AttributeRecord() });
        }

        public Task<IReadOnlyList<AdminAttributeOptionRecord>> GetOptionsAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminAttributeOptionRecord>>(new[] { OptionRecord() });
        }

        public Task<AdminCategoryAttributeRecord?> GetAttributeAsync(
            Guid categoryId,
            Guid attributeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingAttribute);
        }

        public Task<AdminCategoryAttributeRecord> CreateAttributeAsync(
            Guid categoryId,
            AdminCategoryAttributeUpsert command,
            CancellationToken cancellationToken = default)
        {
            LastAttributeUpsert = command;
            return Task.FromResult(AttributeRecord(command.Type, productValuesCount: 0));
        }

        public Task<AdminCategoryAttributeRecord?> UpdateAttributeAsync(
            Guid categoryId,
            Guid attributeId,
            AdminCategoryAttributeUpsert command,
            CancellationToken cancellationToken = default)
        {
            UpdateAttributeCalled = true;
            LastAttributeUpsert = command;
            return Task.FromResult<AdminCategoryAttributeRecord?>(AttributeRecord(command.Type, productValuesCount: 0));
        }

        public Task<bool> DeleteAttributeAsync(
            Guid categoryId,
            Guid attributeId,
            CancellationToken cancellationToken = default)
        {
            DeleteAttributeCalled = true;
            return Task.FromResult(true);
        }

        public Task<AdminAttributeOptionRecord?> GetOptionAsync(
            Guid categoryId,
            Guid attributeId,
            Guid optionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingOption);
        }

        public Task<AdminAttributeOptionRecord> CreateOptionAsync(
            Guid categoryId,
            Guid attributeId,
            AdminAttributeOptionUpsert command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OptionRecord(productValuesCount: 0));
        }

        public Task<AdminAttributeOptionRecord?> UpdateOptionAsync(
            Guid categoryId,
            Guid attributeId,
            Guid optionId,
            AdminAttributeOptionUpsert command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminAttributeOptionRecord?>(OptionRecord(productValuesCount: 0));
        }

        public Task<bool> DeleteOptionAsync(
            Guid categoryId,
            Guid attributeId,
            Guid optionId,
            CancellationToken cancellationToken = default)
        {
            DeleteOptionCalled = true;
            return Task.FromResult(true);
        }

        public Task<AdminCategoryAttributeInheritanceResult> InheritFromParentAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            InheritCalled = true;
            return Task.FromResult(InheritResult);
        }
    }
}
