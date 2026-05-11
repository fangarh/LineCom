using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LineCom.Api.Modules.Catalog;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class CatalogModuleRegistrationTests
{
    [Fact]
    public void AddCatalogModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddCatalogModule();

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddCatalogModule_RegistersReferenceDataAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IPublicCatalogReferenceData>();
        var second = provider.GetRequiredService<IPublicCatalogReferenceData>();

        Assert.Same(first, second);
        Assert.IsType<PublicCatalogReferenceData>(first);
    }

    [Fact]
    public void AddCatalogModule_RegistersPublicCategoryQueryAsScoped()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IPublicCategoryQuery));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperPublicCategoryQuery), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCatalogModule_RegistersPublicProductQueryAsScoped()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IPublicProductQuery));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperPublicProductQuery), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCatalogModule_RegistersAdminHomepageQueryAsScoped()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IAdminHomepageQuery));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperAdminHomepageQuery), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCatalogModule_RegistersAdminProductDuplicateQueryAsScoped()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IAdminProductDuplicateQuery));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperAdminProductDuplicateQuery), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCatalogModule_RegistersAdminCatalogStaffGuardAsScoped()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IAdminCatalogStaffGuard));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(AdminCatalogStaffGuard), descriptor.ImplementationType);
    }

    [Theory]
    [InlineData("in_stock", "В наличии")]
    [InlineData("on_order", "Под заказ")]
    [InlineData("check_availability", "Уточнить")]
    public void ReferenceData_ReturnsPublicAvailabilityLabels(string code, string expectedLabel)
    {
        var referenceData = new PublicCatalogReferenceData();

        var label = referenceData.GetAvailability(code);

        Assert.Equal(new PublicCodeLabelDto(code, expectedLabel), label);
    }

    [Theory]
    [InlineData("coil", "бухта")]
    [InlineData("box", "коробка")]
    [InlineData("piece", "штука")]
    [InlineData("pack", "упаковка")]
    public void ReferenceData_ReturnsPublicSaleUnitLabels(string code, string expectedLabel)
    {
        var referenceData = new PublicCatalogReferenceData();

        var label = referenceData.GetSaleUnit(code);

        Assert.Equal(new PublicCodeLabelDto(code, expectedLabel), label);
    }

    [Theory]
    [InlineData("unknown_availability", true)]
    [InlineData("unknown_sale_unit", false)]
    [InlineData("", true)]
    [InlineData("   ", false)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    public void ReferenceData_MapsInvalidCodesToInvalidFilter(string? code, bool isAvailability)
    {
        var referenceData = new PublicCatalogReferenceData();

        var exception = Assert.Throws<ApiException>(() =>
        {
            if (isAvailability)
            {
                referenceData.GetAvailability(code!);
                return;
            }

            referenceData.GetSaleUnit(code!);
        });

        Assert.Equal("catalog.invalid_filter", exception.Code);
        Assert.Equal("Некорректный параметр фильтра.", exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("IN_STOCK", true)]
    [InlineData("Coil", false)]
    public void ReferenceData_TreatsCodesAsCaseSensitive(string code, bool isAvailability)
    {
        var referenceData = new PublicCatalogReferenceData();

        var exception = Assert.Throws<ApiException>(() =>
        {
            if (isAvailability)
            {
                referenceData.GetAvailability(code);
                return;
            }

            referenceData.GetSaleUnit(code);
        });

        Assert.Equal("catalog.invalid_filter", exception.Code);
        Assert.Equal("Некорректный параметр фильтра.", exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public void ReferenceData_ExposesReadOnlyCodeCollections()
    {
        var referenceData = new PublicCatalogReferenceData();

        var availabilityCodes = Assert.IsAssignableFrom<ICollection<string>>(referenceData.AvailabilityStatusCodes);
        var saleUnitCodes = Assert.IsAssignableFrom<ICollection<string>>(referenceData.SaleUnitCodes);

        Assert.True(availabilityCodes.IsReadOnly);
        Assert.True(saleUnitCodes.IsReadOnly);
    }

    [Fact]
    public void ProductListDefaults_MatchPublicCatalogContract()
    {
        Assert.Equal(1, PublicProductListDefaults.DefaultPage);
        Assert.Equal(24, PublicProductListDefaults.DefaultPageSize);
        Assert.Equal(60, PublicProductListDefaults.MaxPageSize);
        Assert.Equal("category", PublicProductListDefaults.DefaultSort);
        Assert.Equal(
            ["category", "name", "newest"],
            PublicProductSortKeys.All.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ProductSortKeys_AreCaseSensitiveAndReadOnly()
    {
        Assert.True(PublicProductSortKeys.All.Contains("category"));
        Assert.False(PublicProductSortKeys.All.Contains("Category"));

        var collection = Assert.IsAssignableFrom<ICollection<string>>(PublicProductSortKeys.All);
        Assert.True(collection.IsReadOnly);
    }

    [Fact]
    public void PublicCatalogDtos_SerializeWithCamelCaseFields()
    {
        var dto = new PublicCategoryTreeResponse(
        [
            new PublicCategoryTreeItemDto(
                Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1"),
                null,
                "Витая пара",
                "vitaya-para",
                "Витая пара",
                null,
                10,
                true,
                [])
        ]);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"items\"", json);
        Assert.Contains("\"parentId\"", json);
        Assert.Contains("\"sortOrder\"", json);
        Assert.Contains("\"isVisibleInMenu\"", json);
        Assert.DoesNotContain("\"Items\"", json);
        Assert.DoesNotContain("\"ParentId\"", json);
        Assert.DoesNotContain("\"SortOrder\"", json);
        Assert.DoesNotContain("\"IsVisibleInMenu\"", json);
    }

    [Fact]
    public void PublicCatalogDtos_DoNotExposePriceProperties()
    {
        var publicDtoTypes = typeof(PublicCategoryTreeResponse)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "LineCom.Api.Modules.Catalog.DTOs"
                && type.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
            .ToArray();

        Assert.NotEmpty(publicDtoTypes);

        var priceProperties = publicDtoTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.Name.Contains("price", StringComparison.OrdinalIgnoreCase))
            .Select(property => $"{property.DeclaringType?.Name}.{property.Name}")
            .ToArray();

        Assert.Empty(priceProperties);
    }
}
