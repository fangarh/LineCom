using Dapper;
using LineCom.Api.Infrastructure.Database;

namespace LineCom.Api.Modules.Auth.Repositories;

public sealed class DapperUserLoginRepository : IUserLoginRepository
{
    private const string FindByEmailOrPhoneSql = """
        SELECT
            id AS Id,
            name AS Name,
            email AS Email,
            phone AS Phone,
            role AS Role,
            password_hash AS PasswordHash,
            is_active AS IsActive
        FROM users
        WHERE
            (@Email IS NOT NULL AND email = @Email)
            OR (@Phone IS NOT NULL AND phone = @Phone)
        LIMIT 1;
        """;

    private const string FindCurrentUserByIdSql = """
        SELECT
            id AS Id,
            name AS Name,
            email AS Email,
            phone AS Phone,
            role AS Role,
            is_active AS IsActive
        FROM users
        WHERE id = @UserId
        LIMIT 1;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public DapperUserLoginRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LoginUser?> FindByEmailOrPhoneAsync(
        string? email,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<LoginUser>(
            new CommandDefinition(
                FindByEmailOrPhoneSql,
                new { Email = email, Phone = phone },
                cancellationToken: cancellationToken));
    }

    public async Task<CurrentAuthUser?> FindCurrentUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CurrentAuthUser>(
            new CommandDefinition(
                FindCurrentUserByIdSql,
                new { UserId = userId },
                cancellationToken: cancellationToken));
    }
}
