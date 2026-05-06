using System.Reflection;
using DbUp;
using LineCom.DbMigrator.Core;

var connectionString = MigrationConfiguration.GetConnectionString(
    args,
    Environment.GetEnvironmentVariable("LINECOM_CONNECTION_STRING"));

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        Assembly.GetExecutingAssembly(),
        MigrationScripts.IsMigrationScript)
    .JournalToPostgresqlTable("public", "schema_versions")
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Database migrations applied successfully.");
return 0;
