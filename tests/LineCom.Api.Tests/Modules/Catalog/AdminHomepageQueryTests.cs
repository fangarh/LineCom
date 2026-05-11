using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageQueryTests
{
    private static readonly Guid FirstSectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondSectionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UnrelatedSectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void AdminHomepageSql_LoadsSectionsAndItemsWithVisibilityInputs()
    {
        Assert.Contains("FROM homepage_sections", AdminHomepageSql.GetSections);
        Assert.Contains("FROM homepage_section_items item", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product.is_active AS \"ProductIsActive\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product.publish_status AS \"ProductPublishStatus\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product_category.is_active AS \"ProductCategoryIsActive\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("category.is_active AS \"CategoryIsActive\"", AdminHomepageSql.GetSectionItems);
    }

    [Fact]
    public void BuildResponse_PreservesSectionOrderAndGroupsItemsBySection()
    {
        var sections = new[]
        {
            Section(FirstSectionId, "first", 20),
            Section(SecondSectionId, "second", 10)
        };
        var items = new[]
        {
            ProductItem(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), SecondSectionId, "Second item"),
            ProductItem(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), UnrelatedSectionId, "Unrelated item"),
            ProductItem(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), FirstSectionId, "First item")
        };

        var response = DapperAdminHomepageQuery.BuildResponse(sections, items);

        Assert.Equal(["first", "second"], response.Sections.Select(section => section.Code).ToArray());
        Assert.Equal(["First item"], response.Sections[0].Items.Select(item => item.Name).ToArray());
        Assert.Equal(["Second item"], response.Sections[1].Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public void BuildResponse_MapsVisibleProductItemWithSkuSecondaryText()
    {
        var itemId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
        var productId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        var row = ProductItem(
            itemId,
            FirstSectionId,
            "Cable",
            productId: productId,
            slug: "cable",
            sku: "SKU-1",
            productCategoryName: "Category fallback");

        var item = DapperAdminHomepageQuery.BuildResponse([Section(FirstSectionId)], [row]).Sections.Single().Items.Single();

        Assert.Equal(itemId, item.Id);
        Assert.Equal(productId, item.ProductId);
        Assert.Null(item.CategoryId);
        Assert.Equal("Cable", item.Name);
        Assert.Equal("cable", item.Slug);
        Assert.Equal("SKU-1", item.SecondaryText);
        Assert.Equal(5, item.SortOrder);
        Assert.True(item.IsActive);
        Assert.Equal("visible", item.VisibilityStatus);
    }

    [Fact]
    public void BuildResponse_UsesProductCategoryNameWhenSkuMissing()
    {
        var row = ProductItem(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"),
            FirstSectionId,
            "Cable",
            sku: null,
            productCategoryName: "Cable category");

        var item = DapperAdminHomepageQuery.BuildResponse([Section(FirstSectionId)], [row]).Sections.Single().Items.Single();

        Assert.Equal("Cable category", item.SecondaryText);
    }

    [Fact]
    public void BuildResponse_UsesFallbackNamesForMissingJoinedNames()
    {
        var product = ProductItem(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"),
            FirstSectionId,
            null);
        var category = CategoryItem(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007"),
            FirstSectionId,
            null);

        var items = DapperAdminHomepageQuery.BuildResponse([Section(FirstSectionId)], [product, category])
            .Sections.Single().Items;

        Assert.Equal("Товар не найден", items[0].Name);
        Assert.Equal("Категория не найдена", items[1].Name);
    }

    [Theory]
    [MemberData(nameof(ProductVisibilityCases))]
    public void BuildResponse_ResolvesProductVisibilityStatusByPrecedence(
        bool itemIsActive,
        bool? productIsActive,
        string? publishStatus,
        bool? categoryIsActive,
        string expectedStatus)
    {
        var row = ProductItem(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000008"),
            FirstSectionId,
            "Cable",
            itemIsActive: itemIsActive,
            productIsActive: productIsActive,
            publishStatus: publishStatus,
            productCategoryIsActive: categoryIsActive);

        var item = DapperAdminHomepageQuery.BuildResponse([Section(FirstSectionId)], [row]).Sections.Single().Items.Single();

        Assert.Equal(expectedStatus, item.VisibilityStatus);
    }

    [Theory]
    [InlineData(false, false, "item_inactive")]
    [InlineData(true, false, "category_inactive")]
    [InlineData(true, null, "category_inactive")]
    [InlineData(true, true, "visible")]
    public void BuildResponse_ResolvesCategoryVisibilityStatus(
        bool itemIsActive,
        bool? categoryIsActive,
        string expectedStatus)
    {
        var row = CategoryItem(
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009"),
            FirstSectionId,
            "Cables",
            itemIsActive: itemIsActive,
            categoryIsActive: categoryIsActive);

        var item = DapperAdminHomepageQuery.BuildResponse([Section(FirstSectionId)], [row]).Sections.Single().Items.Single();

        Assert.Equal(expectedStatus, item.VisibilityStatus);
    }

    public static TheoryData<bool, bool?, string?, bool?, string> ProductVisibilityCases()
    {
        return new TheoryData<bool, bool?, string?, bool?, string>
        {
            { false, false, "draft", false, "item_inactive" },
            { true, false, "draft", false, "product_inactive" },
            { true, null, "published", true, "product_inactive" },
            { true, true, "draft", false, "product_unpublished" },
            { true, true, null, true, "product_unpublished" },
            { true, true, "published", false, "category_inactive" },
            { true, true, "published", null, "category_inactive" },
            { true, true, "published", true, "visible" }
        };
    }

    private static AdminHomepageSectionRow Section(Guid id, string code = "section", int sortOrder = 10)
    {
        return new AdminHomepageSectionRow(id, code, $"{code} title", "product", 8, sortOrder, true);
    }

    private static AdminHomepageSectionItemRow ProductItem(
        Guid id,
        Guid sectionId,
        string? name,
        Guid? productId = null,
        string? slug = "product-slug",
        string? sku = "SKU",
        bool itemIsActive = true,
        bool? productIsActive = true,
        string? publishStatus = "published",
        string? productCategoryName = "Product category",
        bool? productCategoryIsActive = true)
    {
        return new AdminHomepageSectionItemRow(
            id,
            sectionId,
            productId ?? Guid.Parse("bbbbbbbb-0000-0000-0000-000000000099"),
            null,
            name,
            slug,
            sku,
            productIsActive,
            publishStatus,
            productCategoryName,
            productCategoryIsActive,
            null,
            null,
            null,
            5,
            itemIsActive);
    }

    private static AdminHomepageSectionItemRow CategoryItem(
        Guid id,
        Guid sectionId,
        string? name,
        bool itemIsActive = true,
        bool? categoryIsActive = true)
    {
        return new AdminHomepageSectionItemRow(
            id,
            sectionId,
            null,
            Guid.Parse("cccccccc-0000-0000-0000-000000000099"),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            name,
            "category-slug",
            categoryIsActive,
            10,
            itemIsActive);
    }
}
