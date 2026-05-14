using Dapper;
using LineCom.Api.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminCatalogProductRepository : IAdminCatalogProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminCatalogProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminProductListRecordResponse> GetProductsAsync(
        AdminProductReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var parameters = new
        {
            query.CategoryId,
            query.BrandId,
            query.IsActive,
            query.PublishStatus,
            query.Search,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        };

        var totalItems = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminCatalogProductSql.CountProducts,
                parameters,
                cancellationToken: cancellationToken));
        var items = (await connection.QueryAsync<AdminProductListRecord>(
            new CommandDefinition(
                AdminCatalogProductSql.ListProducts,
                parameters,
                cancellationToken: cancellationToken))).ToArray();

        return new AdminProductListRecordResponse(items, totalItems);
    }

    public async Task<AdminProductDetailRecord?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await QueryProductAsync(connection, id, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProductAttributeValueRecord>> GetProductAttributesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminProductAttributeValueRecord>(
            new CommandDefinition(
                AdminCatalogProductSql.GetProductAttributes,
                new { Id = id },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<AdminProductDuplicateIdentity?> FindDuplicateHardIdentityAsync(
        Guid? excludeProductId,
        string slug,
        string? sku,
        string? externalId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminProductDuplicateIdentity>(
            new CommandDefinition(
                AdminCatalogProductSql.FindDuplicateHardIdentity,
                new { ExcludeProductId = excludeProductId, Slug = slug, Sku = sku, ExternalId = externalId },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminProductReadinessMetadata> GetReadinessMetadataAsync(
        Guid? productId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var category = await connection.QuerySingleOrDefaultAsync<CategoryReadinessRow>(
            new CommandDefinition(
                AdminCatalogProductSql.GetReadinessCategory,
                new { CategoryId = categoryId },
                cancellationToken: cancellationToken));
        if (category is null)
        {
            return new AdminProductReadinessMetadata(
                CategoryExists: false,
                CategoryIsActive: false,
                RequiredAttributes: [],
                InvalidAttributeValueCount: 0);
        }

        var requiredAttributes = (await connection.QueryAsync<AdminProductRequiredAttributeRecord>(
            new CommandDefinition(
                AdminCatalogProductSql.GetReadinessRequiredAttributes,
                new { ProductId = productId, CategoryId = categoryId },
                cancellationToken: cancellationToken))).ToArray();
        var invalidAttributeValueCount = productId is null
            ? 0
            : await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    AdminCatalogProductSql.CountInvalidAttributeValues,
                    new { ProductId = productId, CategoryId = categoryId },
                    cancellationToken: cancellationToken));

        return new AdminProductReadinessMetadata(
            CategoryExists: true,
            category.CategoryIsActive,
            requiredAttributes,
            invalidAttributeValueCount);
    }

    public async Task<AdminProductDetailRecord> CreateProductAsync(
        AdminProductUpsert command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            var id = await connection.QuerySingleAsync<Guid>(
                new CommandDefinition(
                    AdminCatalogProductSql.InsertProduct,
                    AdminProductDapperParameterMapper.ToUpsertParameters(command),
                    cancellationToken: cancellationToken));

            return await QueryRequiredProductAsync(connection, id, cancellationToken);
        }
        catch (PostgresException exception) when (AdminProductPostgresExceptionMapper.TryGetDuplicateField(exception, out var field))
        {
            throw new AdminProductDuplicateIdentityException(field, exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            throw new InvalidAdminProductException(exception);
        }
    }

    public async Task<AdminProductDetailRecord?> UpdateProductAsync(
        Guid id,
        AdminProductUpsert command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogProductSql.UpdateProduct,
                    AdminProductDapperParameterMapper.ToUpsertParameters(command, id),
                    transaction,
                    cancellationToken: cancellationToken));
            if (updatedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (AdminProductPostgresExceptionMapper.TryGetDuplicateField(exception, out var field))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminProductDuplicateIdentityException(field, exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminProductException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<AdminProductDetailRecord?> UpdateProductAttributesAsync(
        Guid id,
        IReadOnlyList<AdminProductAttributeValueUpsert> values,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var lockedProductId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogProductSql.LockProductForAttributeUpdate,
                    new { ProductId = id },
                    transaction,
                    cancellationToken: cancellationToken));
            if (lockedProductId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    AdminCatalogProductSql.DeleteProductAttributes,
                    new { ProductId = id },
                    transaction,
                    cancellationToken: cancellationToken));

            foreach (var value in values)
            {
                var insertedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    new CommandDefinition(
                        AdminCatalogProductSql.InsertProductAttributeValue,
                        AdminProductDapperParameterMapper.ToAttributeValueParameters(id, value),
                        transaction,
                        cancellationToken: cancellationToken));
                if (insertedId is null)
                {
                    throw new InvalidAdminProductException();
                }
            }

            var blockingReadinessIssues = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    AdminCatalogProductSql.CountBlockingAttributeReadinessIssues,
                    new { ProductId = id },
                    transaction,
                    cancellationToken: cancellationToken));
            if (blockingReadinessIssues > 0)
            {
                throw new AdminProductNotReadyException();
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (IsInvalidAttributeUpdate(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminProductException(exception);
        }
        catch (AdminProductNotReadyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (InvalidAdminProductException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<int> CountProductUsageAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                AdminCatalogProductSql.CountProductUsage,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    AdminCatalogProductSql.DeleteProduct,
                    new { Id = id },
                    transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            return deleted > 0;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminProductInUseException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<AdminProductDetailRecord?> QueryProductAsync(
        NpgsqlConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<AdminProductDetailRecord>(
            new CommandDefinition(
                AdminCatalogProductSql.GetProduct,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    private static async Task<AdminProductDetailRecord> QueryRequiredProductAsync(
        NpgsqlConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleAsync<AdminProductDetailRecord>(
            new CommandDefinition(
                AdminCatalogProductSql.GetProduct,
                new { Id = id },
                cancellationToken: cancellationToken));
    }

    private static bool IsInvalidRequest(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.ForeignKeyViolation
            or PostgresErrorCodes.CheckViolation
            or PostgresErrorCodes.RaiseException;
    }

    private static bool IsInvalidAttributeUpdate(PostgresException exception)
    {
        return IsInvalidRequest(exception)
            || exception.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private sealed record CategoryReadinessRow(bool CategoryExists, bool CategoryIsActive);
}
