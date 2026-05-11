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
    public void DeleteProductImage_MarksFileDeletedOnlyWhenUnreferenced()
    {
        Assert.Contains("DELETE FROM product_images", AdminCatalogImageSql.DeleteProductImage);
        Assert.Contains("UPDATE stored_files", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("NOT EXISTS", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("UPDATE product_images", AdminCatalogImageSql.PromoteFirstRemainingProductImage);
    }
}
