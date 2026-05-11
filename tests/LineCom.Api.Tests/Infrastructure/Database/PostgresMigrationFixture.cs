using DbUp;
using LineCom.DbMigrator;
using LineCom.DbMigrator.Core;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class PostgresMigrationFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    public async Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable("LINECOM_TEST_CONNECTION_STRING") ?? string.Empty;
        if (!IsConfigured)
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DROP SCHEMA public CASCADE;
            CREATE SCHEMA public;
            """;
        await command.ExecuteNonQueryAsync();

        var result = DeployChanges.To
            .PostgresqlDatabase(ConnectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(ProgramMarker).Assembly,
                MigrationScripts.IsMigrationScript)
            .JournalToPostgresqlTable("public", "schema_versions")
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw result.Error;
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresMigrationCollection : ICollectionFixture<PostgresMigrationFixture>
{
    public const string Name = "PostgresMigration";
}
