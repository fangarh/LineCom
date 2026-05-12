using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminHomepageRepository : IAdminHomepageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IAdminHomepageQuery _query;

    public DapperAdminHomepageRepository(
        IDbConnectionFactory connectionFactory,
        IAdminHomepageQuery query)
    {
        _connectionFactory = connectionFactory;
        _query = query;
    }

    public async Task<bool> SectionExistsAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<bool>(
            new CommandDefinition(
                AdminHomepageRepositorySql.SectionExists,
                new { SectionId = sectionId },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminHomepageSectionDto?> UpdateSectionAsync(
        Guid sectionId,
        UpdateAdminHomepageSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            var updated = await connection.QuerySingleOrDefaultAsync<AdminHomepageSectionMutationResult>(
                new CommandDefinition(
                    AdminHomepageRepositorySql.UpdateSection,
                    new
                    {
                        SectionId = sectionId,
                        command.Title,
                        command.ItemLimit,
                        command.SortOrder,
                        command.IsActive
                    },
                    cancellationToken: cancellationToken));
            if (updated is null)
            {
                return null;
            }
        }
        catch (PostgresException exception) when (IsConstraintViolation(exception))
        {
            throw new InvalidAdminHomepageMutationException(exception);
        }

        return await FindSectionAsync(sectionId, cancellationToken);
    }

    public async Task<AdminHomepageSectionItemDto?> InsertItemAsync(
        Guid sectionId,
        CreateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default)
    {
        AdminHomepageSectionItemMutationResult? inserted;

        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            inserted = await connection.QuerySingleOrDefaultAsync<AdminHomepageSectionItemMutationResult>(
                new CommandDefinition(
                    AdminHomepageRepositorySql.InsertSectionItem,
                    new
                    {
                        SectionId = sectionId,
                        command.ProductId,
                        command.CategoryId,
                        command.SortOrder,
                        command.IsActive
                    },
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (IsConstraintViolation(exception))
        {
            throw new InvalidAdminHomepageMutationException(exception);
        }

        return inserted is null
            ? null
            : await FindItemAsync(sectionId, inserted.Id, cancellationToken);
    }

    public async Task<AdminHomepageSectionItemDto?> UpdateItemAsync(
        Guid sectionId,
        Guid itemId,
        UpdateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default)
    {
        AdminHomepageSectionItemMutationResult? updated;

        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            updated = await connection.QuerySingleOrDefaultAsync<AdminHomepageSectionItemMutationResult>(
                new CommandDefinition(
                    AdminHomepageRepositorySql.UpdateSectionItem,
                    new
                    {
                        SectionId = sectionId,
                        ItemId = itemId,
                        command.SortOrder,
                        command.IsActive
                    },
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (IsConstraintViolation(exception))
        {
            throw new InvalidAdminHomepageMutationException(exception);
        }

        return updated is null
            ? null
            : await FindItemAsync(sectionId, updated.Id, cancellationToken);
    }

    public async Task<bool> UpdateItemOrderAsync(
        Guid sectionId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            var updatedIds = (await connection.QueryAsync<AdminHomepageSectionItemOrderMutationResult>(
                new CommandDefinition(
                    AdminHomepageRepositorySql.UpdateSectionItemOrder,
                    new { SectionId = sectionId, ItemIds = itemIds.ToArray() },
                    cancellationToken: cancellationToken))).ToArray();

            return updatedIds.Length == itemIds.Count;
        }
        catch (PostgresException exception) when (IsConstraintViolation(exception))
        {
            throw new InvalidAdminHomepageMutationException(exception);
        }
    }

    public async Task<bool> DeleteItemAsync(Guid sectionId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var deleted = await connection.QuerySingleOrDefaultAsync<AdminHomepageSectionItemDeleteMutationResult>(
            new CommandDefinition(
                AdminHomepageRepositorySql.DeleteSectionItem,
                new { SectionId = sectionId, ItemId = itemId },
                cancellationToken: cancellationToken));

        return deleted is not null;
    }

    private async Task<AdminHomepageSectionDto?> FindSectionAsync(
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var response = await _query.GetSectionsAsync(cancellationToken);

        return response.Sections.FirstOrDefault(section => section.Id == sectionId);
    }

    private async Task<AdminHomepageSectionItemDto?> FindItemAsync(
        Guid sectionId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var section = await FindSectionAsync(sectionId, cancellationToken);

        return section?.Items.FirstOrDefault(item => item.Id == itemId);
    }

    private static bool IsConstraintViolation(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.CheckViolation
            or PostgresErrorCodes.ForeignKeyViolation
            or PostgresErrorCodes.UniqueViolation
            or PostgresErrorCodes.RaiseException;
    }
}
