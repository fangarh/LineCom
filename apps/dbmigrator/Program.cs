using System.Reflection;
using DbUp;

var connectionString = GetConnectionString(args);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        Assembly.GetExecutingAssembly(),
        scriptName => scriptName.Contains(".Migrations.") && scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
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

static string GetConnectionString(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        return args[0];
    }

    var fromEnvironment = Environment.GetEnvironmentVariable("LINECOM_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(fromEnvironment))
    {
        return fromEnvironment;
    }

    throw new InvalidOperationException("Connection string is required. Pass it as first argument or set LINECOM_CONNECTION_STRING.");
}
