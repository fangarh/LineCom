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
    public void UpdateBrandLogoRepository_ReadsLogoBeforeCommitting()
    {
        var methodSource = ReadRepositoryMethod(
            "public async Task<AdminBrandLogoRecord?> UpdateBrandLogoAsync",
            "public async Task<bool> DeleteBrandLogoAsync");

        var getLogoIndex = methodSource.IndexOf("AdminCatalogBrandSql.GetBrandLogo", StringComparison.Ordinal);
        var commitIndex = methodSource.IndexOf("transaction.CommitAsync", StringComparison.Ordinal);

        Assert.True(getLogoIndex >= 0);
        Assert.True(commitIndex >= 0);
        Assert.True(getLogoIndex < commitIndex);
    }

    [Fact]
    public void DeleteBrandLogo_ClearsLogoFileId()
    {
        Assert.Contains("SET logo_file_id = NULL", AdminCatalogBrandSql.ClearBrandLogo);
        Assert.Contains("WHERE id = @BrandId", AdminCatalogBrandSql.ClearBrandLogo);
    }

    private static string ReadRepositoryMethod(string startMarker, string endMarker)
    {
        var repositorySource = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "apps",
                "api",
                "Modules",
                "Catalog",
                "Repositories",
                "DapperAdminCatalogBrandRepository.cs"));
        var startIndex = repositorySource.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = repositorySource.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.True(startIndex >= 0);
        Assert.True(endIndex > startIndex);

        return repositorySource[startIndex..endIndex];
    }
}
