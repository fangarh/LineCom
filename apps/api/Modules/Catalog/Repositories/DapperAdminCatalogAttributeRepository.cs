using Dapper;
using LineCom.Api.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminCatalogAttributeRepository : IAdminCatalogAttributeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminCatalogAttributeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AdminCategoryAttributeRecord>> GetAttributesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminCategoryAttributeRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.ListAttributes,
                new { CategoryId = categoryId },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<IReadOnlyList<AdminAttributeOptionRecord>> GetOptionsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminAttributeOptionRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.ListOptions,
                new { CategoryId = categoryId },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<AdminCategoryAttributeRecord?> GetAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminCategoryAttributeRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.GetAttribute,
                new { CategoryId = categoryId, AttributeId = attributeId },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminCategoryAttributeRecord> CreateAttributeAsync(
        Guid categoryId,
        AdminCategoryAttributeUpsert command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            var attributeId = await connection.QuerySingleAsync<Guid>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.InsertAttribute,
                    ToAttributeParameters(categoryId, Guid.Empty, command),
                    cancellationToken: cancellationToken));

            return await QueryRequiredAttributeAsync(connection, categoryId, attributeId, cancellationToken);
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            throw new AdminCatalogAttributeDuplicateException(exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            throw new InvalidAdminCatalogAttributeException(exception);
        }
    }

    public async Task<AdminCategoryAttributeRecord?> UpdateAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        AdminCategoryAttributeUpsert command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.UpdateAttribute,
                    ToAttributeParameters(categoryId, attributeId, command),
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
            throw new AdminCatalogAttributeDuplicateException(exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminCatalogAttributeException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetAttributeAsync(categoryId, attributeId, cancellationToken);
    }

    public async Task<bool> DeleteAttributeAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    AdminCatalogAttributeSql.DeleteAttribute,
                    new { CategoryId = categoryId, AttributeId = attributeId },
                    transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            return deleted > 0;
        }
        catch (PostgresException exception) when (IsInUse(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminCatalogAttributeInUseException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AdminAttributeOptionRecord?> GetOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AdminAttributeOptionRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.GetOption,
                new { CategoryId = categoryId, AttributeId = attributeId, OptionId = optionId },
                cancellationToken: cancellationToken));
    }

    public async Task<AdminAttributeOptionRecord> CreateOptionAsync(
        Guid categoryId,
        Guid attributeId,
        AdminAttributeOptionUpsert command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            var optionId = await connection.QuerySingleAsync<Guid>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.InsertOption,
                    ToOptionParameters(categoryId, attributeId, Guid.Empty, command),
                    cancellationToken: cancellationToken));

            return await QueryRequiredOptionAsync(connection, categoryId, attributeId, optionId, cancellationToken);
        }
        catch (PostgresException exception) when (IsUniqueViolation(exception))
        {
            throw new AdminCatalogAttributeDuplicateException(exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            throw new InvalidAdminCatalogAttributeException(exception);
        }
    }

    public async Task<AdminAttributeOptionRecord?> UpdateOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        AdminAttributeOptionUpsert command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.UpdateOption,
                    ToOptionParameters(categoryId, attributeId, optionId, command),
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
            throw new AdminCatalogAttributeDuplicateException(exception);
        }
        catch (PostgresException exception) when (IsInvalidRequest(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new InvalidAdminCatalogAttributeException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetOptionAsync(categoryId, attributeId, optionId, cancellationToken);
    }

    public async Task<bool> DeleteOptionAsync(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleted = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.DeleteOption,
                    new { CategoryId = categoryId, AttributeId = attributeId, OptionId = optionId },
                    transaction,
                    cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            return deleted > 0;
        }
        catch (PostgresException exception) when (IsInUse(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new AdminCatalogAttributeInUseException(exception);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AdminCategoryAttributeInheritanceResult> InheritFromParentAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var copied = (await connection.QueryAsync<InheritedAttributeRow>(
                new CommandDefinition(
                    AdminCatalogAttributeSql.InheritMissingAttributes,
                    new { CategoryId = categoryId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();

            var copiedAttributes = copied
                .Where(row => row.ParentAttributeId is not null && row.ChildAttributeId is not null)
                .ToArray();

            if (copiedAttributes.Length > 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        AdminCatalogAttributeSql.InheritOptionsForCopiedAttributes,
                        new
                        {
                            CopiedAttributeIds = copiedAttributes.Select(row => row.ChildAttributeId!.Value).ToArray(),
                            ParentAttributeIds = copiedAttributes.Select(row => row.ParentAttributeId!.Value).ToArray()
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);

            return new AdminCategoryAttributeInheritanceResult(
                copiedAttributes.Length,
                copied.FirstOrDefault()?.Skipped ?? 0);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static object ToAttributeParameters(
        Guid categoryId,
        Guid attributeId,
        AdminCategoryAttributeUpsert command)
    {
        return new
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            command.Name,
            command.Code,
            command.Type,
            command.Unit,
            command.IsRequired,
            command.IsFilterable,
            command.IsComparable,
            command.IsVisibleInProduct,
            command.IsSeoImportant,
            command.IsUsedInGeneratedName,
            command.SortOrder,
            command.IsActive
        };
    }

    private static object ToOptionParameters(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        AdminAttributeOptionUpsert command)
    {
        return new
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            OptionId = optionId,
            command.Value,
            command.Slug,
            command.NormalizedValue,
            command.SortOrder,
            command.IsActive
        };
    }

    private static async Task<AdminCategoryAttributeRecord> QueryRequiredAttributeAsync(
        Npgsql.NpgsqlConnection connection,
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleAsync<AdminCategoryAttributeRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.GetAttribute,
                new { CategoryId = categoryId, AttributeId = attributeId },
                cancellationToken: cancellationToken));
    }

    private static async Task<AdminAttributeOptionRecord> QueryRequiredOptionAsync(
        Npgsql.NpgsqlConnection connection,
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleAsync<AdminAttributeOptionRecord>(
            new CommandDefinition(
                AdminCatalogAttributeSql.GetOption,
                new { CategoryId = categoryId, AttributeId = attributeId, OptionId = optionId },
                cancellationToken: cancellationToken));
    }

    private static bool IsUniqueViolation(PostgresException exception)
    {
        return exception.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsInvalidRequest(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.ForeignKeyViolation
            or PostgresErrorCodes.CheckViolation
            or PostgresErrorCodes.RaiseException;
    }

    private static bool IsInUse(PostgresException exception)
    {
        return exception.SqlState == PostgresErrorCodes.ForeignKeyViolation;
    }

    private sealed record InheritedAttributeRow(
        Guid? ParentAttributeId,
        Guid? ChildAttributeId,
        int Skipped);
}
