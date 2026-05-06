using LineCom.DbMigrator.Core;

namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class DbMigratorConfigurationTests
{
    [Fact]
    public void GetConnectionString_ReturnsFirstArgument_WhenArgumentProvided()
    {
        var connectionString = MigrationConfiguration.GetConnectionString(
            ["Host=from-argument"],
            "Host=from-environment");

        Assert.Equal("Host=from-argument", connectionString);
    }

    [Fact]
    public void GetConnectionString_ReturnsEnvironmentValue_WhenArgumentMissing()
    {
        var connectionString = MigrationConfiguration.GetConnectionString(
            [],
            "Host=from-environment");

        Assert.Equal("Host=from-environment", connectionString);
    }

    [Fact]
    public void GetConnectionString_ReturnsEnvironmentValue_WhenArgumentBlank()
    {
        var connectionString = MigrationConfiguration.GetConnectionString(
            ["   "],
            "Host=from-environment");

        Assert.Equal("Host=from-environment", connectionString);
    }

    [Fact]
    public void GetConnectionString_Throws_WhenArgumentsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MigrationConfiguration.GetConnectionString(null!, "Host=from-environment"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetConnectionString_Throws_WhenArgumentAndEnvironmentValueMissing(string? environmentConnectionString)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MigrationConfiguration.GetConnectionString([], environmentConnectionString));

        Assert.Equal(
            "Connection string is required. Pass it as first argument or set LINECOM_CONNECTION_STRING.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetConnectionString_Throws_WhenArgumentBlankAndEnvironmentValueMissing(string argument)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MigrationConfiguration.GetConnectionString([argument], null));

        Assert.Equal(
            "Connection string is required. Pass it as first argument or set LINECOM_CONNECTION_STRING.",
            exception.Message);
    }

    [Theory]
    [InlineData("LineCom.DbMigrator.Migrations.001_extensions.sql", true)]
    [InlineData("LineCom.DbMigrator.Migrations.002_catalog_foundation.SQL", true)]
    [InlineData("LineCom.DbMigrator.Other.001_extensions.sql", false)]
    [InlineData("LineCom.DbMigrator.migrations.001_extensions.sql", false)]
    [InlineData("LineCom.DbMigrator.Migrations.001_extensions.txt", false)]
    public void IsMigrationScript_FiltersEmbeddedSqlMigrationResources(string scriptName, bool expected)
    {
        Assert.Equal(expected, MigrationScripts.IsMigrationScript(scriptName));
    }

    [Fact]
    public void IsMigrationScript_Throws_WhenScriptNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MigrationScripts.IsMigrationScript(null!));
    }

    [Fact]
    public void DbMigratorProject_EmbedsSqlMigrationScripts()
    {
        var projectFile = Path.Combine(FindRepositoryRoot(), "apps", "dbmigrator", "LineCom.DbMigrator.csproj");

        var projectXml = File.ReadAllText(projectFile);

        Assert.Contains("<EmbeddedResource Include=\"Migrations/**/*.sql\" />", projectXml);
    }

    [Fact]
    public void CatalogFoundationMigration_FileExists()
    {
        var migrationFile = Path.Combine(
            FindRepositoryRoot(),
            "apps",
            "dbmigrator",
            "Migrations",
            "002_catalog_foundation.sql");

        Assert.True(File.Exists(migrationFile), $"Expected migration file '{migrationFile}' to exist.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
