using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperAdminHomepageQuery : IAdminHomepageQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminHomepageQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sections = (await connection.QueryAsync<AdminHomepageSectionRow>(
            new CommandDefinition(AdminHomepageSql.GetSections, cancellationToken: cancellationToken))).ToArray();
        var items = (await connection.QueryAsync<AdminHomepageSectionItemRow>(
            new CommandDefinition(AdminHomepageSql.GetSectionItems, cancellationToken: cancellationToken))).ToArray();

        return BuildResponse(sections, items);
    }

    internal static AdminHomepageSectionsResponse BuildResponse(
        IReadOnlyList<AdminHomepageSectionRow> sections,
        IReadOnlyList<AdminHomepageSectionItemRow> items)
    {
        var itemsBySection = items.ToLookup(item => item.SectionId);
        return new AdminHomepageSectionsResponse(sections
            .Select(section => new AdminHomepageSectionDto(
                section.Id,
                section.Code,
                section.Title,
                section.Type,
                section.ItemLimit,
                section.SortOrder,
                section.IsActive,
                itemsBySection[section.Id].Select(BuildItem).ToArray()))
            .ToArray());
    }

    private static AdminHomepageSectionItemDto BuildItem(AdminHomepageSectionItemRow row)
    {
        if (row.ProductId is not null)
        {
            return new AdminHomepageSectionItemDto(
                row.Id,
                row.ProductId,
                null,
                row.ProductName ?? "Товар не найден",
                row.ProductSlug,
                row.ProductSku ?? row.ProductCategoryName,
                row.SortOrder,
                row.IsActive,
                ResolveProductVisibilityStatus(row));
        }

        return new AdminHomepageSectionItemDto(
            row.Id,
            null,
            row.CategoryId,
            row.CategoryName ?? "Категория не найдена",
            row.CategorySlug,
            null,
            row.SortOrder,
            row.IsActive,
            ResolveCategoryVisibilityStatus(row));
    }

    private static string ResolveProductVisibilityStatus(AdminHomepageSectionItemRow row)
    {
        if (!row.IsActive)
        {
            return "item_inactive";
        }

        if (row.ProductIsActive != true)
        {
            return "product_inactive";
        }

        if (!string.Equals(row.ProductPublishStatus, "published", StringComparison.Ordinal))
        {
            return "product_unpublished";
        }

        if (row.ProductCategoryIsActive != true)
        {
            return "category_inactive";
        }

        return "visible";
    }

    private static string ResolveCategoryVisibilityStatus(AdminHomepageSectionItemRow row)
    {
        if (!row.IsActive)
        {
            return "item_inactive";
        }

        return row.CategoryIsActive == true ? "visible" : "category_inactive";
    }
}
