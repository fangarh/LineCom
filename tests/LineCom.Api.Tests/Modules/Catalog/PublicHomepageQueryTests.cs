using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicHomepageQueryTests
{
    [Fact]
    public void PublicHomepageSql_OnlyReturnsActiveVisibleItems()
    {
        Assert.Contains("section.is_active = TRUE", PublicHomepageSql.GetSections);
        Assert.Contains("item.is_active = TRUE", PublicHomepageSql.GetSectionItems);
        Assert.Contains("product.publish_status = 'published'", PublicHomepageSql.GetSectionItems);
        Assert.Contains("product.is_active = TRUE", PublicHomepageSql.GetSectionItems);
        Assert.Contains("product.slug IS NOT NULL", PublicHomepageSql.GetSectionItems);
        Assert.Contains("product_category.is_active = TRUE", PublicHomepageSql.GetSectionItems);
        Assert.Contains("category.is_active = TRUE", PublicHomepageSql.GetSectionItems);
        Assert.Contains("category.slug IS NOT NULL", PublicHomepageSql.GetSectionItems);
    }
}
