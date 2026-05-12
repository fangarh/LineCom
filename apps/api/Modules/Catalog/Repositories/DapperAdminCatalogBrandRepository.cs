using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Infrastructure.Storage;
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

    public async Task<AdminBrandLogoRecord?> UpdateBrandLogoAsync(
        Guid brandId,
        LocalStoredFileDraft file,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        AdminBrandLogoRecord? logo;

        try
        {
            var previousLogoRows = (await connection.QueryAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogBrandSql.GetBrandLogoFileId,
                    new { BrandId = brandId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();
            if (previousLogoRows.Length == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var previousLogoFileId = previousLogoRows[0];

            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogBrandSql.InsertStoredFile,
                file,
                transaction,
                cancellationToken: cancellationToken));

            await connection.QuerySingleAsync<Guid>(new CommandDefinition(
                AdminCatalogBrandSql.UpdateBrandLogo,
                new { BrandId = brandId, LogoFileId = file.Id },
                transaction,
                cancellationToken: cancellationToken));

            if (previousLogoFileId is not null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced,
                    new { StoredFileId = previousLogoFileId.Value },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            logo = await connection.QuerySingleOrDefaultAsync<AdminBrandLogoRecord>(
                new CommandDefinition(
                    AdminCatalogBrandSql.GetBrandLogo,
                    new { BrandId = brandId },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return logo;
    }

    public async Task<bool> DeleteBrandLogoAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var previousLogoRows = (await connection.QueryAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogBrandSql.GetBrandLogoFileId,
                    new { BrandId = brandId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();
            if (previousLogoRows.Length == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var previousLogoFileId = previousLogoRows[0];
            if (previousLogoFileId is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            await connection.QuerySingleAsync<Guid>(new CommandDefinition(
                AdminCatalogBrandSql.ClearBrandLogo,
                new { BrandId = brandId, PreviousLogoFileId = previousLogoFileId.Value },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced,
                new { StoredFileId = previousLogoFileId.Value },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<AdminBrandLogoRecord?> GetBrandLogoAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminBrandLogoRecord>(
            new CommandDefinition(
                AdminCatalogBrandSql.GetBrandLogo,
                new { BrandId = brandId },
                cancellationToken: cancellationToken));
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
