using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
