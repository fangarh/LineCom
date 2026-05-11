using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogProductServiceTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BrandId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid AttributeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetProductsAsync_NormalizesFiltersAndKeepsInactiveProducts()
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            ListItems =
            [
                ProductListRecord(isActive: false)
            ]
        };
        var service = CreateService("seller", repository);

        var response = await service.GetProductsAsync(
            new DefaultHttpContext(),
            new AdminProductListQuery(0, 1000, CategoryId, BrandId, false, " published ", " cable "),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
        Assert.Equal(1, repository.LastListQuery.Page);
        Assert.Equal(60, repository.LastListQuery.PageSize);
        Assert.Equal(CategoryId, repository.LastListQuery.CategoryId);
        Assert.Equal(BrandId, repository.LastListQuery.BrandId);
        Assert.False(repository.LastListQuery.IsActive);
        Assert.Equal("published", repository.LastListQuery.PublishStatus);
        Assert.Equal("cable", repository.LastListQuery.Search);
        Assert.False(Assert.Single(response.Items).IsActive);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsInactiveProductDetailWithAttributes()
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            Detail = ProductDetailRecord(isActive: false),
            Attributes =
            [
                AttributeValueRecord(valueText: "PVC")
            ]
        };
        var service = CreateService("seller", repository);

        var response = await service.GetProductAsync(new DefaultHttpContext(), ProductId, CancellationToken.None);

        Assert.Equal(ProductId, response.Id);
        Assert.False(response.IsActive);
        Assert.Equal("Cable", response.Name);
        Assert.Equal("Category", response.CategoryName);
        Assert.Equal("Brand", response.BrandName);
        Assert.Equal(1, response.Images.ImagesCount);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), response.Images.MainImageFileId);
        Assert.Equal("PVC", Assert.Single(response.Attributes).ValueText);
        Assert.Contains(response.Readiness.Issues, issue => issue.Code == "product_inactive");
    }

    [Theory]
    [InlineData("   ", "cable", "coil", "305 m")]
    [InlineData("Cable", "   ", "coil", "305 m")]
    [InlineData("Cable", "cable", "   ", "305 m")]
    [InlineData("Cable", "cable", "coil", "   ")]
    public async Task CreateProductAsync_RejectsBlankRequiredFields(
        string name,
        string slug,
        string saleUnit,
        string unitQuantity)
    {
        var service = CreateService("seller", new CapturingAdminCatalogProductRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateProductAsync(
                new DefaultHttpContext(),
                ValidCommand(name: name, slug: slug, saleUnit: saleUnit, unitQuantity: unitQuantity),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task CreateProductAsync_NormalizesTextBeforeRepositoryCall()
    {
        var repository = new CapturingAdminCatalogProductRepository();
        var service = CreateService("admin", repository);

        await service.CreateProductAsync(
            new DefaultHttpContext(),
            ValidCommand(
                name: " Cable ",
                slug: " cable ",
                sku: " LC-1 ",
                externalId: " EXT-1 ",
                publishStatus: " draft "),
            CancellationToken.None);

        Assert.NotNull(repository.LastUpsert);
        Assert.Equal("Cable", repository.LastUpsert.Name);
        Assert.Equal("cable", repository.LastUpsert.Slug);
        Assert.Equal("LC-1", repository.LastUpsert.Sku);
        Assert.Equal("EXT-1", repository.LastUpsert.ExternalId);
        Assert.Equal("draft", repository.LastUpsert.PublishStatus);
    }

    [Theory]
    [InlineData("   ", "cable", "coil", "305 m")]
    [InlineData("Cable", "   ", "coil", "305 m")]
    [InlineData("Cable", "cable", "   ", "305 m")]
    [InlineData("Cable", "cable", "coil", "   ")]
    public async Task UpdateProductAsync_RejectsBlankRequiredFields(
        string name,
        string slug,
        string saleUnit,
        string unitQuantity)
    {
        var service = CreateService("seller", new CapturingAdminCatalogProductRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductAsync(
                new DefaultHttpContext(),
                ProductId,
                ValidCommand(name: name, slug: slug, saleUnit: saleUnit, unitQuantity: unitQuantity),
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task UpdateProductAsync_NormalizesTextBeforeRepositoryCall()
    {
        var repository = new CapturingAdminCatalogProductRepository();
        var service = CreateService("admin", repository);

        await service.UpdateProductAsync(
            new DefaultHttpContext(),
            ProductId,
            ValidCommand(
                name: " Cable ",
                slug: " cable ",
                sku: " LC-1 ",
                externalId: " EXT-1 ",
                publishStatus: " draft "),
            CancellationToken.None);

        Assert.NotNull(repository.LastUpsert);
        Assert.Equal("Cable", repository.LastUpsert.Name);
        Assert.Equal("cable", repository.LastUpsert.Slug);
        Assert.Equal("LC-1", repository.LastUpsert.Sku);
        Assert.Equal("EXT-1", repository.LastUpsert.ExternalId);
        Assert.Equal("draft", repository.LastUpsert.PublishStatus);
    }

    [Theory]
    [InlineData("slug", "admin_catalog.slug_already_exists")]
    [InlineData("sku", "admin_catalog.sku_already_exists")]
    [InlineData("external_id", "admin_catalog.external_id_already_exists")]
    public async Task CreateProductAsync_DuplicateHardIdentity_MapsToConflictError(
        string field,
        string expectedCode)
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            DuplicateIdentity = new AdminProductDuplicateIdentity(ProductId, field)
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateProductAsync(new DefaultHttpContext(), ValidCommand(), CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateProductAsync_PublishedProductWithBlockingIssues_ThrowsProductNotReady()
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            ReadinessMetadata = new AdminProductReadinessMetadata(
                CategoryExists: true,
                CategoryIsActive: true,
                RequiredAttributes:
                [
                    new AdminProductRequiredAttributeRecord(
                        AttributeId,
                        "jacket",
                        "Jacket",
                        "text",
                        ValueText: null,
                        ValueNumber: null,
                        ValueBoolean: null,
                        AttributeOptionId: null)
                ],
                InvalidAttributeValueCount: 0)
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductAsync(
                new DefaultHttpContext(),
                ProductId,
                ValidCommand(publishStatus: "published"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.product_not_ready", exception.Code);
        Assert.Equal("\u0422\u043e\u0432\u0430\u0440 \u043d\u0435 \u0433\u043e\u0442\u043e\u0432 \u043a \u043f\u0443\u0431\u043b\u0438\u043a\u0430\u0446\u0438\u0438.", exception.Message);
        Assert.Null(repository.LastUpsert);
    }

    [Fact]
    public async Task UpdateProductAsync_MissingProductWithConflictingPublishedPayload_ThrowsProductNotFound()
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            Detail = null,
            DuplicateIdentity = new AdminProductDuplicateIdentity(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "slug"),
            ReadinessMetadata = new AdminProductReadinessMetadata(
                CategoryExists: true,
                CategoryIsActive: true,
                RequiredAttributes:
                [
                    new AdminProductRequiredAttributeRecord(
                        AttributeId,
                        "jacket",
                        "Jacket",
                        "text",
                        ValueText: null,
                        ValueNumber: null,
                        ValueBoolean: null,
                        AttributeOptionId: null)
                ],
                InvalidAttributeValueCount: 0)
        };
        var service = CreateService("seller", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductAsync(
                new DefaultHttpContext(),
                ProductId,
                ValidCommand(publishStatus: "published"),
                CancellationToken.None));

        Assert.Equal("admin_catalog.product_not_found", exception.Code);
        Assert.Equal(1, repository.GetProductCallCount);
        Assert.False(repository.FindDuplicateHardIdentityCalled);
        Assert.False(repository.GetReadinessMetadataCalled);
        Assert.Null(repository.LastUpsert);
    }

    [Fact]
    public async Task DeleteProductAsync_WithRequestOrHomepageUsage_ThrowsEntityInUse()
    {
        var repository = new CapturingAdminCatalogProductRepository
        {
            ProductUsageCount = 2
        };
        var service = CreateService("admin", repository);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.DeleteProductAsync(new DefaultHttpContext(), ProductId, CancellationToken.None));

        Assert.Equal("admin_catalog.entity_in_use", exception.Code);
        Assert.False(repository.DeleteCalled);
    }

    private static AdminCatalogProductService CreateService(
        string role,
        CapturingAdminCatalogProductRepository repository)
    {
        return new AdminCatalogProductService(
            new RoleAdminCatalogStaffGuard(role),
            repository);
    }

    private static UpsertAdminProductCommand ValidCommand(
        string? name = "Cable",
        string? slug = "cable",
        string? saleUnit = "coil",
        string? unitQuantity = "305 m",
        string? sku = "LC-1",
        string? externalId = null,
        string? publishStatus = "draft")
    {
        return new UpsertAdminProductCommand(
            CategoryId,
            BrandId,
            name,
            slug,
            sku,
            externalId,
            "Description",
            "Short",
            "in_stock",
            saleUnit,
            unitQuantity,
            publishStatus,
            true,
            "SEO",
            "SEO description",
            "H1",
            10);
    }

    private static AdminProductListRecord ProductListRecord(bool isActive = true)
    {
        return new AdminProductListRecord(
            ProductId,
            "Cable",
            "cable",
            "LC-1",
            null,
            CategoryId,
            "Category",
            "category",
            CategoryIsActive: true,
            BrandId,
            "Brand",
            "published",
            isActive,
            "in_stock",
            10,
            MissingRequiredAttributeCount: 0,
            InvalidAttributeValueCount: 0);
    }

    private static AdminProductDetailRecord ProductDetailRecord(bool isActive = true)
    {
        return new AdminProductDetailRecord(
            ProductId,
            CategoryId,
            "Category",
            CategoryIsActive: true,
            BrandId,
            "Brand",
            "Cable",
            "cable",
            "LC-1",
            null,
            "Description",
            "Short",
            "in_stock",
            "coil",
            "305 m",
            "published",
            isActive,
            "SEO",
            "SEO description",
            "H1",
            10,
            ImagesCount: 1,
            MainImageFileId: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            MissingRequiredAttributeCount: 0,
            InvalidAttributeValueCount: 0);
    }

    private static AdminProductAttributeValueRecord AttributeValueRecord(string? valueText)
    {
        return new AdminProductAttributeValueRecord(
            AttributeId,
            "jacket",
            "Jacket",
            "text",
            null,
            valueText,
            null,
            null,
            null,
            null,
            IsRequired: true,
            IsValidValue: valueText is not null);
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
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Staff User",
                "staff@example.com",
                null,
                _role));
        }
    }

    private sealed class CapturingAdminCatalogProductRepository : IAdminCatalogProductRepository
    {
        public AdminProductReadListQuery? LastListQuery { get; private set; }
        public AdminProductUpsert? LastUpsert { get; private set; }
        public bool DeleteCalled { get; private set; }
        public IReadOnlyList<AdminProductListRecord> ListItems { get; init; } = [ProductListRecord()];
        public AdminProductDetailRecord? Detail { get; init; } = ProductDetailRecord();
        public IReadOnlyList<AdminProductAttributeValueRecord> Attributes { get; init; } = [];
        public AdminProductDuplicateIdentity? DuplicateIdentity { get; init; }
        public int ProductUsageCount { get; init; }
        public bool DeleteResult { get; init; } = true;
        public int GetProductCallCount { get; private set; }
        public bool FindDuplicateHardIdentityCalled { get; private set; }
        public bool GetReadinessMetadataCalled { get; private set; }
        public AdminProductReadinessMetadata ReadinessMetadata { get; init; } = new(
            CategoryExists: true,
            CategoryIsActive: true,
            RequiredAttributes: [],
            InvalidAttributeValueCount: 0);

        public Task<AdminProductListRecordResponse> GetProductsAsync(
            AdminProductReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastListQuery = query;
            return Task.FromResult(new AdminProductListRecordResponse(ListItems, ListItems.Count));
        }

        public Task<AdminProductDetailRecord?> GetProductAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetProductCallCount++;
            return Task.FromResult(Detail);
        }

        public Task<IReadOnlyList<AdminProductAttributeValueRecord>> GetProductAttributesAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Attributes);
        }

        public Task<AdminProductDuplicateIdentity?> FindDuplicateHardIdentityAsync(
            Guid? excludeProductId,
            string slug,
            string? sku,
            string? externalId,
            CancellationToken cancellationToken = default)
        {
            FindDuplicateHardIdentityCalled = true;
            return Task.FromResult(DuplicateIdentity);
        }

        public Task<AdminProductReadinessMetadata> GetReadinessMetadataAsync(
            Guid? productId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            GetReadinessMetadataCalled = true;
            return Task.FromResult(ReadinessMetadata);
        }

        public Task<AdminProductDetailRecord> CreateProductAsync(
            AdminProductUpsert command,
            CancellationToken cancellationToken = default)
        {
            LastUpsert = command;
            return Task.FromResult(ProductDetailRecord());
        }

        public Task<AdminProductDetailRecord?> UpdateProductAsync(
            Guid id,
            AdminProductUpsert command,
            CancellationToken cancellationToken = default)
        {
            LastUpsert = command;
            return Task.FromResult<AdminProductDetailRecord?>(ProductDetailRecord());
        }

        public Task<int> CountProductUsageAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProductUsageCount);
        }

        public Task<bool> DeleteProductAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.FromResult(DeleteResult);
        }
    }
}
