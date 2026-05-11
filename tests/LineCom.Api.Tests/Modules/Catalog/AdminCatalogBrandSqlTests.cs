using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogBrandSqlTests
{
    [Fact]
    public void ListBrands_SelectsAdminFieldsAndUsageCounts()
    {
        Assert.Contains("FROM brands brand", AdminCatalogBrandSql.ListBrands);
        Assert.Contains("COUNT(product.id)::int AS \"ProductsCount\"", AdminCatalogBrandSql.ListBrands);
    }

    [Fact]
    public void GetBrand_SelectsLogoFile()
    {
        Assert.Contains("brand.logo_file_id AS \"LogoFileId\"", AdminCatalogBrandSql.GetBrand);
    }

    [Fact]
    public void DeleteBrand_PhysicallyDeletesBrands()
    {
        Assert.Contains("DELETE FROM brands", AdminCatalogBrandSql.DeleteBrand);
        Assert.Contains("NOT EXISTS", AdminCatalogBrandSql.DeleteBrand);
        Assert.Contains("product.brand_id = brand.id", AdminCatalogBrandSql.DeleteBrand);
    }
}
