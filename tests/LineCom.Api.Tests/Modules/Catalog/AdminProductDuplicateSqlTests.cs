using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminProductDuplicateSqlTests
{
    [Fact]
    public void FindCandidates_UsesHardIdentityMatchesAndTrigramSimilarity()
    {
        Assert.Contains("product.sku = @Sku", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.external_id = @ExternalId", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.slug = @Slug", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("similarity(product.name, @Name)", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("similarity(product.slug, @Slug)", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.primary_category_id = @CategoryId", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.brand_id = @BrandId", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("LIMIT @Limit", AdminProductDuplicateSql.FindCandidates);
    }

    [Fact]
    public void FindCandidates_UsesNonNullSimilarityScoreWithHardIdentityMatchesAsExact()
    {
        Assert.Contains("CASE", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 1", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains(
            "WHEN @ExternalId IS NOT NULL AND product.external_id = @ExternalId THEN 1",
            AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("WHEN @Slug IS NOT NULL AND product.slug = @Slug THEN 1", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains(
            "ELSE COALESCE(GREATEST(similarity(product.name, @Name), similarity(product.slug, @Slug)), 0)",
            AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("END::numeric AS \"Similarity\"", AdminProductDuplicateSql.FindCandidates);
    }
}
