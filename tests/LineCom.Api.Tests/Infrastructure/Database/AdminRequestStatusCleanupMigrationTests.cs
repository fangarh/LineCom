namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class AdminRequestStatusCleanupMigrationTests
{
    private static readonly string CleanupSql = ReadMigration("006_admin_request_status_cleanup.sql");

    [Fact]
    public void AdminRequestStatusCleanup_MapsQuotedRequestStatusesToInProgress()
    {
        Assert.Contains(
            "UPDATE requests SET status = 'in_progress' WHERE status = 'quoted';",
            CleanupSql);
    }

    [Fact]
    public void AdminRequestStatusCleanup_MapsQuotedHistoryStatusesToInProgress()
    {
        Assert.Contains("UPDATE request_history", CleanupSql);
        Assert.Contains("old_status = CASE WHEN old_status = 'quoted' THEN 'in_progress' ELSE old_status END", CleanupSql);
        Assert.Contains("new_status = CASE WHEN new_status = 'quoted' THEN 'in_progress' ELSE new_status END", CleanupSql);
        Assert.Contains("WHERE old_status = 'quoted' OR new_status = 'quoted';", CleanupSql);
    }

    [Theory]
    [InlineData("ALTER TABLE requests DROP CONSTRAINT IF EXISTS ck_requests_status;")]
    [InlineData("ALTER TABLE request_history DROP CONSTRAINT IF EXISTS ck_request_history_old_status;")]
    [InlineData("ALTER TABLE request_history DROP CONSTRAINT IF EXISTS ck_request_history_new_status;")]
    [InlineData("ADD CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    [InlineData("ADD CONSTRAINT ck_request_history_old_status CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    [InlineData("ADD CONSTRAINT ck_request_history_new_status CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    public void AdminRequestStatusCleanup_RecreatesReleaseStatusConstraints(string expectedSql)
    {
        Assert.Contains(expectedSql, CleanupSql);
    }

    private static string ReadMigration(string fileName)
    {
        var migrationFile = Path.Combine(FindRepositoryRoot(), "apps", "dbmigrator", "Migrations", fileName);

        return File.ReadAllText(migrationFile);
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
