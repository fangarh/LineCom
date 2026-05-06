using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public interface IDbConnectionFactory
{
    ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
