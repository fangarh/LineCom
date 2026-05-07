using Dapper;
using LineCom.Api.Infrastructure.Database;

namespace LineCom.Api.Modules.Requests.Repositories;

public sealed class DapperRequestNumberRepository : IRequestNumberRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperRequestNumberRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetNextSequenceAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                RequestNumberSql.GetNextSequence,
                new { Year = year },
                cancellationToken: cancellationToken));
    }
}
