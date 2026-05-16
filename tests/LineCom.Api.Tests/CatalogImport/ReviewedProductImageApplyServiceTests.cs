using LineCom.CatalogImport.Core.Images;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class ReviewedProductImageApplyServiceTests
{
    [Fact]
    public void PlanSkipsProductWithExistingImagesUnlessAllowed()
    {
        var items = new[]
        {
            new ReviewedProductImageManifestItem("101-a", "101", "image.png", new string('a', 64), "image/png", true, "requires-permission")
        };
        var states = new Dictionary<string, ReviewedProductImageProductState>(StringComparer.Ordinal)
        {
            ["101"] = new ReviewedProductImageProductState(Guid.NewGuid(), "Кабель UTP", 1, true)
        };

        var plan = ReviewedProductImageApplyPlanner.Plan(items, states, allowAddToProductsWithExistingImages: false);

        Assert.Empty(plan.Apply);
        Assert.Contains(plan.Skip, item => item.ExternalId == "101" && item.Reason.Contains("already has images", StringComparison.OrdinalIgnoreCase));
    }
}
