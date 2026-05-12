using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperPublicHomepageQuery : IPublicHomepageQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperPublicHomepageQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PublicHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sections = (await connection.QueryAsync<PublicHomepageSectionRow>(
            new CommandDefinition(PublicHomepageSql.GetSections, cancellationToken: cancellationToken))).ToArray();
        var items = (await connection.QueryAsync<PublicHomepageSectionItemRow>(
            new CommandDefinition(PublicHomepageSql.GetSectionItems, cancellationToken: cancellationToken))).ToArray();

        return BuildResponse(sections, items);
    }

    internal static PublicHomepageSectionsResponse BuildResponse(
        IReadOnlyList<PublicHomepageSectionRow> sections,
        IReadOnlyList<PublicHomepageSectionItemRow> items)
    {
        var itemsBySection = items.ToLookup(item => item.SectionId);
        return new PublicHomepageSectionsResponse(sections
            .Select(section => new PublicHomepageSectionDto(
                section.Code,
                section.Title,
                section.Type,
                itemsBySection[section.Id]
                    .Take(section.ItemLimit)
                    .Select(BuildItem)
                    .ToArray()))
            .ToArray());
    }

    private static PublicHomepageSectionItemDto BuildItem(PublicHomepageSectionItemRow row)
    {
        return new PublicHomepageSectionItemDto(
            row.Id,
            row.ProductId,
            row.CategoryId,
            row.Name,
            row.Slug,
            row.SecondaryText);
    }
}

internal sealed record PublicHomepageSectionRow(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit);

internal sealed record PublicHomepageSectionItemRow(
    Guid Id,
    Guid SectionId,
    Guid? ProductId,
    Guid? CategoryId,
    string Name,
    string? Slug,
    string? SecondaryText,
    int SortOrder);
