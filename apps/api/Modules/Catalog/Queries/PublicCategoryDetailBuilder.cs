using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

internal static class PublicCategoryDetailBuilder
{
    public static PublicCategoryDetailDto Build(IReadOnlyCollection<PublicCategoryDetailRow> rows)
    {
        var category = rows.SingleOrDefault(row => row.Depth == 0);
        if (category is null)
        {
            throw PublicCatalogErrors.CategoryNotFound();
        }

        var breadcrumbs = rows
            .OrderByDescending(row => row.Depth)
            .Select(row => new PublicBreadcrumbDto(row.Name, row.Slug))
            .ToArray();

        return new PublicCategoryDetailDto(
            category.Id,
            category.ParentId,
            category.Name,
            category.Slug,
            category.Description,
            category.H1,
            new PublicSeoDto(
                category.SeoTitle,
                category.SeoDescription,
                $"/catalog/{category.Slug}"),
            breadcrumbs);
    }
}
