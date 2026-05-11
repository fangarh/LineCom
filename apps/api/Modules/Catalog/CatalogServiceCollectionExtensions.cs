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
        services.AddScoped<IAdminProductDuplicateQuery, DapperAdminProductDuplicateQuery>();
        services.AddScoped<IPublicCategoryQuery, DapperPublicCategoryQuery>();
        services.AddScoped<IPublicProductQuery, DapperPublicProductQuery>();
        services.AddSingleton<IPublicCatalogReferenceData, PublicCatalogReferenceData>();

        return services;
    }
}
