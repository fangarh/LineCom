using Dapper;
using LineCom.Api.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminCatalogCategoryRepository : IAdminCatalogCategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminCatalogCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminCategoryListRecordResponse> GetCategoriesAsync(
        AdminCategoryReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var parameters = new
        {
            query.ParentId,
            query.Search,
            query.IsActive,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        };

        var totalItems = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminCatalogCategorySql.CountCategories,
                parameters,
                cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<AdminCategoryRecord>(
            new CommandDefinition(
                AdminCatalogCategorySql.ListCategories,
                parameters,
                cancellationToken: cancellationToken))).ToArray();

        return new AdminCategoryListRecordResponse(items, totalItems);
    }

    public async Task<AdminCategoryRecord?> GetCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminCategoryRecord>(
            new CommandDefinition(
                AdminCatalogCategorySql.GetCategory,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminCategoryRecord> CreateCategoryAsync(
        AdminCategoryUpsert command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleAsync<AdminCategoryRecord>(
                new CommandDefinition(
                    AdminCatalogCategorySql.InsertCategory,
                    command,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            throw new AdminCategorySlugAlreadyExistsException(exception);
        }
        catch (PostgresException exception) when (IsInvalidCategoryParent(exception))
        {
            throw new InvalidAdminCategoryParentException(exception);
        }
    }

    public async Task<AdminCategoryRecord?> UpdateCategoryAsync(
        Guid id,
        AdminCategoryUpsert command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogCategorySql.UpdateCategory,
                    new
                    {
                        Id = id,
                        command.ParentId,
                        command.Name,
                        command.Slug,
                        command.Description,
                        command.SeoTitle,
                        command.SeoDescription,
                        command.H1,
                        command.SortOrder,
                        command.IsActive,
                        command.IsVisibleInMenu
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            if (updatedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminCategorySlugAlreadyExistsException(exception);
        }
        catch (PostgresException exception) when (IsInvalidCategoryParent(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminCategoryParentException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetCategoryAsync(id, cancellationToken);
    }

    public async Task<AdminCategoryRecord?> MoveCategoryAsync(
        Guid id,
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogCategorySql.MoveCategory,
                    new { Id = id, ParentId = parentId },
                    transaction,
                    cancellationToken: cancellationToken));
            if (updatedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (IsInvalidCategoryParent(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminCategoryParentException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetCategoryAsync(id, cancellationToken);
    }

    public async Task<AdminCategoryRecord?> SortCategoryAsync(
        Guid id,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogCategorySql.SortCategory,
                    new { Id = id, SortOrder = sortOrder },
                    transaction,
                    cancellationToken: cancellationToken));
            if (updatedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetCategoryAsync(id, cancellationToken);
    }

    public async Task<int> CountCategoryUsageAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminCatalogCategorySql.CountCategoryUsage,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    AdminCatalogCategorySql.DeleteCategory,
                    new { Id = id },
                    transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            return deleted > 0;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminCategoryInUseException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool IsUniqueViolation(PostgresException exception)
    {
        return exception.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsInvalidCategoryParent(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation;
    }
}
