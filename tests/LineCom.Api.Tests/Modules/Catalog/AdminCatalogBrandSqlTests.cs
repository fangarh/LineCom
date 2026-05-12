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

    [Fact]
    public void BrandLogoSql_RegistersBrandLogoFileAndMarksPreviousLogoDeletedWhenUnreferenced()
    {
        Assert.Contains("INSERT INTO stored_files", AdminCatalogBrandSql.InsertStoredFile);
        Assert.Contains("'active'", AdminCatalogBrandSql.InsertStoredFile);
        Assert.Contains("UPDATE brands", AdminCatalogBrandSql.UpdateBrandLogo);
        Assert.Contains("logo_file_id = @LogoFileId", AdminCatalogBrandSql.UpdateBrandLogo);
        Assert.Contains("UPDATE stored_files", AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced);
        Assert.Contains("NOT EXISTS", AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced);
    }

    [Fact]
    public void DeleteBrandLogo_ClearsLogoFileId()
    {
        Assert.Contains("SET logo_file_id = NULL", AdminCatalogBrandSql.ClearBrandLogo);
        Assert.Contains("WHERE id = @BrandId", AdminCatalogBrandSql.ClearBrandLogo);
    }
}
