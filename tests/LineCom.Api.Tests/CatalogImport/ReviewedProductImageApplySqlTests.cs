using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageApplySqlTests
{
    [Fact]
    public void SelectExistingImagesUsesExternalIdAndCountsCurrentProductImages()
    {
        Assert.Contains("WHERE product.external_id = @ExternalId", ReviewedProductImageApplySql.SelectProductImageState);
        Assert.Contains("COUNT(image.id)", ReviewedProductImageApplySql.SelectProductImageState);
    }

    [Fact]
    public void InsertProductImageDoesNotClearExistingMainImage()
    {
        Assert.DoesNotContain("DELETE FROM product_images", ReviewedProductImageApplySql.InsertProductImage);
        Assert.DoesNotContain("UPDATE product_images", ReviewedProductImageApplySql.InsertProductImage);
        Assert.Contains("ON CONFLICT (product_id, stored_file_id) DO NOTHING", ReviewedProductImageApplySql.InsertProductImage);
    }
}
