namespace LineCom.DbMigrator.Core;

public static class MigrationConfiguration
{
    public static string GetConnectionString(string[] args, string? environmentConnectionString)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0];
        }

        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        throw new InvalidOperationException("Connection string is required. Pass it as first argument or set LINECOM_CONNECTION_STRING.");
    }
}
