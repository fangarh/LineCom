using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicCategoryTreeBuilder
{
    public static IReadOnlyList<PublicCategoryTreeItemDto> Build(IReadOnlyCollection<PublicCategoryRow> rows)
    {
        var categoriesByParent = rows.ToLookup(category => category.ParentId);

        return BuildCategoryTreeItems(categoriesByParent, parentId: null, ancestorIds: new HashSet<Guid>());
    }

    private static IReadOnlyList<PublicCategoryTreeItemDto> BuildCategoryTreeItems(
        ILookup<Guid?, PublicCategoryRow> categoriesByParent,
        Guid? parentId,
        IReadOnlySet<Guid> ancestorIds)
    {
        var items = new List<PublicCategoryTreeItemDto>();

        foreach (var category in categoriesByParent[parentId])
        {
            if (ancestorIds.Contains(category.Id))
            {
                continue;
            }

            var nextAncestorIds = new HashSet<Guid>(ancestorIds)
            {
                category.Id
            };

            items.Add(new PublicCategoryTreeItemDto(
                category.Id,
                category.ParentId,
                category.Name,
                category.Slug,
                category.H1,
                category.Description,
                category.SortOrder,
                category.IsVisibleInMenu,
                BuildCategoryTreeItems(categoriesByParent, category.Id, nextAncestorIds)));
        }

        return items;
    }
}
