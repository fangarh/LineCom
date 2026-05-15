using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageRepositorySqlTests
{
    [Fact]
    public void SectionExists_ChecksHomepageSectionById()
    {
        Assert.Contains("FROM homepage_sections section", AdminHomepageRepositorySql.SectionExists);
        Assert.Contains("WHERE section.id = @SectionId", AdminHomepageRepositorySql.SectionExists);
    }

    [Fact]
    public void UpdateSection_ReturnsSectionMutationResultShape()
    {
        AssertMutationRecordShape<AdminHomepageSectionMutationResult>(
            nameof(AdminHomepageSectionMutationResult.Id),
            nameof(AdminHomepageSectionMutationResult.Code),
            nameof(AdminHomepageSectionMutationResult.Title),
            nameof(AdminHomepageSectionMutationResult.Type),
            nameof(AdminHomepageSectionMutationResult.ItemLimit),
            nameof(AdminHomepageSectionMutationResult.SortOrder),
            nameof(AdminHomepageSectionMutationResult.IsActive));

        Assert.Contains("UPDATE homepage_sections", AdminHomepageRepositorySql.UpdateSection);
        Assert.Contains("WHERE id = @SectionId", AdminHomepageRepositorySql.UpdateSection);
        Assert.Contains("RETURNING", AdminHomepageRepositorySql.UpdateSection);
        Assert.Contains("id AS \"Id\"", AdminHomepageRepositorySql.UpdateSection);
        Assert.Contains("is_active AS \"IsActive\"", AdminHomepageRepositorySql.UpdateSection);
    }

    [Fact]
    public void InsertSectionItem_ReturnsItemMutationResultShapeAndValidatesTarget()
    {
        AssertMutationRecordShape<AdminHomepageSectionItemMutationResult>(
            nameof(AdminHomepageSectionItemMutationResult.Id),
            nameof(AdminHomepageSectionItemMutationResult.ProductId),
            nameof(AdminHomepageSectionItemMutationResult.CategoryId),
            nameof(AdminHomepageSectionItemMutationResult.SortOrder),
            nameof(AdminHomepageSectionItemMutationResult.IsActive));

        Assert.Contains("INSERT INTO homepage_section_items", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("WHERE section.id = @SectionId", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("num_nonnulls(@ProductId, @CategoryId) = 1", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("num_nonnulls(product_id, category_id)", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("id AS \"Id\"", AdminHomepageRepositorySql.InsertSectionItem);
    }

    [Fact]
    public void InsertSectionItem_ReturnsExistingItemForDuplicateTarget()
    {
        Assert.Contains("existing AS", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM existing)", AdminHomepageRepositorySql.InsertSectionItem);
        Assert.Contains("UNION ALL", AdminHomepageRepositorySql.InsertSectionItem);
    }

    [Fact]
    public void UpdateSectionItem_ReturnsItemMutationResultShape()
    {
        Assert.Contains("UPDATE homepage_section_items", AdminHomepageRepositorySql.UpdateSectionItem);
        Assert.Contains("WHERE section_id = @SectionId", AdminHomepageRepositorySql.UpdateSectionItem);
        Assert.Contains("AND id = @ItemId", AdminHomepageRepositorySql.UpdateSectionItem);
        Assert.Contains("RETURNING", AdminHomepageRepositorySql.UpdateSectionItem);
        Assert.Contains("sort_order AS \"SortOrder\"", AdminHomepageRepositorySql.UpdateSectionItem);
    }

    [Fact]
    public void UpdateSectionItemOrder_UpdatesOnlyRequestedSectionItems()
    {
        AssertMutationRecordShape<AdminHomepageSectionItemOrderMutationResult>(
            nameof(AdminHomepageSectionItemOrderMutationResult.Id));

        Assert.Contains("UPDATE homepage_section_items", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("unnest(CAST(@ItemIds AS uuid[])) WITH ORDINALITY", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("item.section_id = @SectionId", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("RETURNING item.id", AdminHomepageRepositorySql.UpdateSectionItemOrder);
    }

    [Fact]
    public void UpdateSectionItemOrder_GuardsAgainstPartialUpdate()
    {
        Assert.Contains("requested_items AS", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("valid_section_items AS", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("SELECT COUNT(*) FROM valid_section_items", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("SELECT COUNT(*) FROM requested_items", AdminHomepageRepositorySql.UpdateSectionItemOrder);
        Assert.Contains("UPDATE homepage_section_items item", AdminHomepageRepositorySql.UpdateSectionItemOrder);
    }

    [Fact]
    public void DeleteSectionItem_DeletesOnlyRequestedSectionItem()
    {
        AssertMutationRecordShape<AdminHomepageSectionItemDeleteMutationResult>(
            nameof(AdminHomepageSectionItemDeleteMutationResult.Id));

        Assert.Contains("DELETE FROM homepage_section_items", AdminHomepageRepositorySql.DeleteSectionItem);
        Assert.Contains("WHERE section_id = @SectionId", AdminHomepageRepositorySql.DeleteSectionItem);
        Assert.Contains("AND id = @ItemId", AdminHomepageRepositorySql.DeleteSectionItem);
        Assert.Contains("RETURNING id", AdminHomepageRepositorySql.DeleteSectionItem);
    }

    private static void AssertMutationRecordShape<TRecord>(params string[] expectedProperties)
    {
        var properties = typeof(TRecord)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(expectedProperties, properties);
    }
}
