using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicCategoryTreeBuilderTests
{
    [Fact]
    public void Build_ReturnsEmptyItems_WhenRowsAreEmpty()
    {
        var items = PublicCategoryTreeBuilder.Build([]);

        Assert.Empty(items);
    }

    [Fact]
    public void Build_KeepsOnlyReachableRootTree_WhenParentRowIsMissing()
    {
        var rootId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var missingParentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orphanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var items = PublicCategoryTreeBuilder.Build(
        [
            CreateRow(rootId, null, "Витая пара", "vitaya-para"),
            CreateRow(orphanId, missingParentId, "Скрытая ветка", "hidden-branch")
        ]);

        var root = Assert.Single(items);
        Assert.Equal(rootId, root.Id);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void Build_PreservesQueryOrderWithinEachLevel()
    {
        var rootId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var firstChildId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondChildId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var items = PublicCategoryTreeBuilder.Build(
        [
            CreateRow(rootId, null, "Витая пара", "vitaya-para"),
            CreateRow(firstChildId, rootId, "A", "a"),
            CreateRow(secondChildId, rootId, "B", "b")
        ]);

        var root = Assert.Single(items);
        Assert.Equal([firstChildId, secondChildId], root.Children.Select(child => child.Id).ToArray());
    }

    [Fact]
    public void Build_DoesNotRecurseForever_WhenCycleAppearsInReachableRows()
    {
        var rootId = Guid.Parse("6f830f45-0502-4cbf-8cda-f0ac8c74e7f1");
        var childId = Guid.Parse("dcd4f577-6076-4283-b256-30ea0822a3b2");

        var items = PublicCategoryTreeBuilder.Build(
        [
            CreateRow(rootId, null, "Витая пара", "vitaya-para"),
            CreateRow(childId, rootId, "Кабель U/UTP", "u-utp"),
            CreateRow(rootId, childId, "Витая пара", "vitaya-para")
        ]);

        var root = Assert.Single(items);
        var child = Assert.Single(root.Children);
        Assert.Empty(child.Children);
    }

    private static PublicCategoryRow CreateRow(Guid id, Guid? parentId, string name, string slug)
    {
        return new PublicCategoryRow(
            id,
            parentId,
            name,
            slug,
            H1: null,
            Description: null,
            SortOrder: 10,
            IsVisibleInMenu: true);
    }
}
