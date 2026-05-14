using Dapper;
using LineCom.Api.Infrastructure.Database;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperStorageDiagnosticsRepository : IStorageDiagnosticsRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperStorageDiagnosticsRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<StorageDiagnosticsStoredFileRecord>> ListStoredFilesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StorageDiagnosticsStoredFileRecord>(
            new CommandDefinition(
                StorageDiagnosticsSql.ListStoredFiles,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
