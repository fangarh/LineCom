namespace LineCom.DbMigrator.Core;

public static class MigrationScripts
{
    public static bool IsMigrationScript(string scriptName)
    {
        ArgumentNullException.ThrowIfNull(scriptName);

        return scriptName.Contains(".Migrations.", StringComparison.Ordinal)
            && scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
    }
}
