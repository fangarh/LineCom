using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;

namespace LineCom.Api.Modules.Catalog;

public static class CatalogServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<IAdminHomepageQuery, DapperAdminHomepageQuery>();
        services.AddScoped<IAdminCatalogStaffGuard, AdminCatalogStaffGuard>();
        services.AddScoped<IAdminCatalogCategoryRepository, DapperAdminCatalogCategoryRepository>();
        services.AddScoped<IAdminCatalogCategoryService, AdminCatalogCategoryService>();
        services.AddScoped<IAdminCatalogBrandRepository, DapperAdminCatalogBrandRepository>();
        services.AddScoped<IAdminCatalogBrandService, AdminCatalogBrandService>();
        services.AddScoped<IAdminCatalogAttributeRepository, DapperAdminCatalogAttributeRepository>();
        services.AddScoped<IAdminCatalogAttributeService, AdminCatalogAttributeService>();
        services.AddScoped<IAdminCatalogProductRepository, DapperAdminCatalogProductRepository>();
        services.AddScoped<IAdminCatalogImageRepository, DapperAdminCatalogImageRepository>();
        services.AddScoped<IAdminCatalogProductService, AdminCatalogProductService>();
        services.AddScoped<IAdminProductDuplicateQuery, DapperAdminProductDuplicateQuery>();
        services.AddScoped<IPublicCategoryQuery, DapperPublicCategoryQuery>();
        services.AddScoped<IPublicProductQuery, DapperPublicProductQuery>();
        services.AddSingleton<IPublicCatalogReferenceData, PublicCatalogReferenceData>();

        return services;
    }
}
