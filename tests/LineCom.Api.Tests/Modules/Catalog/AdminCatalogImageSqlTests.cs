using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogImageSqlTests
{
    [Fact]
    public void ListProductImages_SelectsActiveStoredFilesInDisplayOrder()
    {
        Assert.Contains("FROM product_images image", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("stored_file.status = 'active'", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("stored_file.purpose = 'product_image'", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("'/' || stored_file.storage_key AS \"Url\"", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("ORDER BY image.is_main DESC, image.sort_order, image.id", AdminCatalogImageSql.ListProductImages);
    }

    [Fact]
    public void InsertProductImage_RegistersStoredFileAndDefaultsFirstImageToMain()
    {
        Assert.Contains("INSERT INTO stored_files", AdminCatalogImageSql.InsertStoredFile);
        Assert.Contains("INSERT INTO product_images", AdminCatalogImageSql.InsertProductImage);
        Assert.Contains("COALESCE(MAX(sort_order), 0) + 10", AdminCatalogImageSql.InsertProductImage);
        Assert.Contains("NOT EXISTS", AdminCatalogImageSql.InsertProductImage);
    }

    [Fact]
    public void AddProductImagesSql_LocksProductBeforeInsertingImages()
    {
        Assert.Contains("FROM products product", AdminCatalogImageSql.LockProductForImageUpdate);
        Assert.Contains("WHERE product.id = @ProductId", AdminCatalogImageSql.LockProductForImageUpdate);
        Assert.Contains("FOR UPDATE", AdminCatalogImageSql.LockProductForImageUpdate);
    }

    [Fact]
    public void AddProductImagesRepository_LocksProductBeforeInsertingImages()
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
                "DapperAdminCatalogImageRepository.cs"));

        Assert.True(
            repositorySource.IndexOf("AdminCatalogImageSql.LockProductForImageUpdate", StringComparison.Ordinal)
                < repositorySource.IndexOf("AdminCatalogImageSql.InsertStoredFile", StringComparison.Ordinal));
    }

    [Fact]
    public void InsertProductImage_DefaultMainCheckRequiresActiveProductImagePurpose()
    {
        Assert.Contains("existing_file.status = 'active'", AdminCatalogImageSql.InsertProductImage);
        Assert.Contains("existing_file.purpose = 'product_image'", AdminCatalogImageSql.InsertProductImage);
    }

    [Fact]
    public void UpdateProductImage_UpdatesOnlyActiveProductImages()
    {
        Assert.Contains("FROM stored_files stored_file", AdminCatalogImageSql.UpdateProductImage);
        Assert.Contains("stored_file.status = 'active'", AdminCatalogImageSql.UpdateProductImage);
        Assert.Contains("stored_file.purpose = 'product_image'", AdminCatalogImageSql.UpdateProductImage);
    }

    [Fact]
    public void DeleteProductImage_MarksFileDeletedOnlyWhenUnreferenced()
    {
        Assert.Contains("INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id", AdminCatalogImageSql.GetProductImageForDelete);
        Assert.Contains("stored_file.status = 'active'", AdminCatalogImageSql.GetProductImageForDelete);
        Assert.Contains("stored_file.purpose = 'product_image'", AdminCatalogImageSql.GetProductImageForDelete);
        Assert.Contains("DELETE FROM product_images", AdminCatalogImageSql.DeleteProductImage);
        Assert.Contains("UPDATE stored_files", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("stored_file.purpose = 'product_image'", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("NOT EXISTS", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("UPDATE product_images", AdminCatalogImageSql.PromoteFirstRemainingProductImage);
    }

    [Fact]
    public void AddProductImagesRepository_ReadsInsertedImagesBeforeCommitting()
    {
        var methodSource = ReadRepositoryMethod(
            "public async Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync",
            "public async Task<AdminProductImageRecord?> UpdateProductImageAsync");

        var listIndex = methodSource.IndexOf("AdminCatalogImageSql.ListProductImages", StringComparison.Ordinal);
        var commitIndex = methodSource.IndexOf("transaction.CommitAsync", StringComparison.Ordinal);

        Assert.True(listIndex >= 0);
        Assert.True(commitIndex >= 0);
        Assert.True(listIndex < commitIndex);
    }

    [Fact]
    public void PromoteFirstRemainingProductImage_IgnoresInactiveMainImagesWhenCheckingMainExists()
    {
        Assert.Contains(
            "INNER JOIN stored_files main_file ON main_file.id = main_image.stored_file_id",
            AdminCatalogImageSql.PromoteFirstRemainingProductImage);
        Assert.Contains("main_file.status = 'active'", AdminCatalogImageSql.PromoteFirstRemainingProductImage);
        Assert.Contains("main_file.purpose = 'product_image'", AdminCatalogImageSql.PromoteFirstRemainingProductImage);
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
                "DapperAdminCatalogImageRepository.cs"));
        var startIndex = repositorySource.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = repositorySource.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.True(startIndex >= 0);
        Assert.True(endIndex > startIndex);

        return repositorySource[startIndex..endIndex];
    }
}
