using Dapper;
using LineCom.Api.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Modules.Auth.Repositories;

public sealed class DapperUserRegistrationRepository : IUserRegistrationRepository
{
    private const string UniqueViolationSqlState = "23505";

    private const string InsertCustomerSql = """
        INSERT INTO users (
            name,
            email,
            phone,
            password_hash,
            role,
            is_active
        )
        VALUES (
            @Name,
            @Email,
            @Phone,
            @PasswordHash,
            @Role,
            @IsActive
        )
        RETURNING
            id AS Id,
            name AS Name,
            email AS Email,
            phone AS Phone,
            role AS Role;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public DapperUserRegistrationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RegisteredUser> CreateCustomerAsync(
        NewUserRegistration registration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleAsync<RegisteredUser>(
                new CommandDefinition(
                    InsertCustomerSql,
                    registration,
                    cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolationSqlState)
        {
            throw new DuplicateUserContactException();
        }
    }
}
