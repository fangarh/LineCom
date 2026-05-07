using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using Npgsql;

namespace LineCom.Api.Modules.Account.Repositories;

public sealed class DapperAccountProfileRepository : IAccountProfileRepository
{
    private const string UniqueViolationSqlState = "23505";

    private const string FindOrganizationSql = """
        SELECT
            name AS Name,
            inn AS Inn,
            contact_person AS ContactPerson,
            phone AS Phone,
            email AS Email,
            comment AS Comment
        FROM organizations
        WHERE user_id = @UserId
        LIMIT 1;
        """;

    private const string UpdateProfileSql = """
        UPDATE users
        SET
            name = @Name,
            email = @Email,
            phone = @Phone
        WHERE id = @UserId
        RETURNING
            id AS Id,
            name AS Name,
            email AS Email,
            phone AS Phone,
            role AS Role;
        """;

    private const string UpsertOrganizationSql = """
        INSERT INTO organizations (
            user_id,
            name,
            inn,
            contact_person,
            phone,
            email,
            comment
        )
        VALUES (
            @UserId,
            @Name,
            @Inn,
            @ContactPerson,
            @Phone,
            @Email,
            @Comment
        )
        ON CONFLICT (user_id) DO UPDATE
        SET
            name = EXCLUDED.name,
            inn = EXCLUDED.inn,
            contact_person = EXCLUDED.contact_person,
            phone = EXCLUDED.phone,
            email = EXCLUDED.email,
            comment = EXCLUDED.comment
        RETURNING
            name AS Name,
            inn AS Inn,
            contact_person AS ContactPerson,
            phone AS Phone,
            email AS Email,
            comment AS Comment;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAccountProfileRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AccountOrganizationRecord?> FindOrganizationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AccountOrganizationRecord>(
            new CommandDefinition(
                FindOrganizationSql,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(
        Guid userId,
        AccountProfileUpdate profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleAsync<CurrentUserDto>(
                new CommandDefinition(
                    UpdateProfileSql,
                    new
                    {
                        UserId = userId,
                        profile.Name,
                        profile.Email,
                        profile.Phone
                    },
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolationSqlState)
        {
            throw new DuplicateUserContactException();
        }
    }

    public async Task<AccountOrganizationRecord> UpsertOrganizationAsync(
        Guid userId,
        AccountOrganizationUpsert organization,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<AccountOrganizationRecord>(
            new CommandDefinition(
                UpsertOrganizationSql,
                new
                {
                    UserId = userId,
                    organization.Name,
                    organization.Inn,
                    organization.ContactPerson,
                    organization.Phone,
                    organization.Email,
                    organization.Comment
                },
                cancellationToken: cancellationToken));
    }
}
