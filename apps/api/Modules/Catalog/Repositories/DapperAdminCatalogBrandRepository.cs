using Dapper;
using LineCom.Api.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminCatalogBrandRepository : IAdminCatalogBrandRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminCatalogBrandRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminBrandListRecordResponse> GetBrandsAsync(
        AdminBrandReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var parameters = new
        {
            query.Search,
            query.IsActive,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        };

        var totalItems = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminCatalogBrandSql.CountBrands,
                parameters,
                cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<AdminBrandRecord>(
            new CommandDefinition(
                AdminCatalogBrandSql.ListBrands,
                parameters,
                cancellationToken: cancellationToken))).ToArray();

        return new AdminBrandListRecordResponse(items, totalItems);
    }

    public async Task<AdminBrandRecord?> GetBrandAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminBrandRecord>(
            new CommandDefinition(
                AdminCatalogBrandSql.GetBrand,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminBrandRecord> CreateBrandAsync(
        AdminBrandUpsert command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleAsync<AdminBrandRecord>(
                new CommandDefinition(
                    AdminCatalogBrandSql.InsertBrand,
                    command,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            throw new AdminBrandSlugAlreadyExistsException(exception);
        }
        catch (PostgresException exception) when (IsInvalidLogo(exception))
        {
            throw new InvalidAdminBrandLogoException(exception);
        }
    }

    public async Task<AdminBrandRecord?> UpdateBrandAsync(
        Guid id,
        AdminBrandUpsert command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogBrandSql.UpdateBrand,
                    new
                    {
                        Id = id,
                        command.Name,
                        command.Slug,
                        command.Description,
                        command.SeoTitle,
                        command.SeoDescription,
                        command.LogoFileId,
                        command.IsActive
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
            throw new AdminBrandSlugAlreadyExistsException(exception);
        }
        catch (PostgresException exception) when (IsInvalidLogo(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminBrandLogoException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetBrandAsync(id, cancellationToken);
    }

    public async Task<AdminBrandRecord> QuickCreateBrandAsync(
        AdminBrandQuickCreate command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleAsync<AdminBrandRecord>(
                new CommandDefinition(
                    AdminCatalogBrandSql.QuickCreateBrand,
                    command,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            throw new AdminBrandSlugAlreadyExistsException(exception);
        }
    }

    public async Task<bool> DeleteBrandAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    AdminCatalogBrandSql.DeleteBrand,
                    new { Id = id },
                    transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            return deleted > 0;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminBrandInUseException(exception);
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

    private static bool IsInvalidLogo(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.ForeignKeyViolation
            or PostgresErrorCodes.CheckViolation
            or PostgresErrorCodes.RaiseException;
    }
}
