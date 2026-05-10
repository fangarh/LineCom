namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class RequestCoreMigrationTests
{
    private static readonly string RequestCoreSql = ReadMigration("004_requests.sql");

    [Theory]
    [InlineData("requests")]
    [InlineData("request_items")]
    [InlineData("request_history")]
    [InlineData("request_number_counters")]
    public void RequestCore_CreatesExpectedReleaseTables(string tableName)
    {
        Assert.Contains($"CREATE TABLE {tableName} (", RequestCoreSql);
    }

    [Theory]
    [InlineData("CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    [InlineData("CONSTRAINT ck_requests_source CHECK (source IN ('cart', 'quick_order'))")]
    [InlineData("CONSTRAINT ck_request_items_quantity_positive CHECK (quantity > 0)")]
    [InlineData("CONSTRAINT ck_request_history_event_type CHECK (event_type IN ('created', 'status_changed', 'comment_added', 'items_changed'))")]
    [InlineData("CONSTRAINT ck_request_history_old_status CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    [InlineData("CONSTRAINT ck_request_history_new_status CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
    public void RequestCore_ConstrainsReleaseValues(string expectedConstraint)
    {
        Assert.Contains(expectedConstraint, RequestCoreSql);
    }

    [Theory]
    [InlineData("CREATE UNIQUE INDEX ux_requests_number ON requests (number);")]
    [InlineData("CREATE UNIQUE INDEX ux_requests_number_year_sequence ON requests (number_year, number_sequence);")]
    [InlineData("CREATE INDEX ix_requests_user_id_created_at ON requests (user_id, created_at DESC);")]
    [InlineData("CREATE INDEX ix_request_items_request_id_sort_order ON request_items (request_id, sort_order);")]
    [InlineData("CREATE INDEX ix_request_history_request_id_created_at ON request_history (request_id, created_at);")]
    public void RequestCore_DefinesRequestIndexes(string expectedIndex)
    {
        Assert.Contains(expectedIndex, RequestCoreSql);
    }

    [Fact]
    public void RequestCore_StoresSnapshotsForRequestsAndItems()
    {
        Assert.Contains("customer_name text NOT NULL", RequestCoreSql);
        Assert.Contains("customer_email citext NULL", RequestCoreSql);
        Assert.Contains("organization_name text NULL", RequestCoreSql);
        Assert.Contains("product_name text NOT NULL", RequestCoreSql);
        Assert.Contains("product_slug text NOT NULL", RequestCoreSql);
        Assert.Contains("sale_unit text NOT NULL", RequestCoreSql);
        Assert.Contains("unit_quantity text NOT NULL", RequestCoreSql);
        Assert.Contains("customer_comment text NULL", RequestCoreSql);
    }

    [Fact]
    public void RequestCore_DefinesNumberCounterYearAndSequence()
    {
        Assert.Contains("year integer PRIMARY KEY", RequestCoreSql);
        Assert.Contains("next_sequence integer NOT NULL DEFAULT 1", RequestCoreSql);
        Assert.Contains("CONSTRAINT ck_request_number_counters_next_sequence_positive CHECK (next_sequence > 0)", RequestCoreSql);
    }

    [Fact]
    public void RequestCore_DefinesUpdatedAtTriggers()
    {
        Assert.Contains("CREATE TRIGGER trg_requests_set_updated_at", RequestCoreSql);
        Assert.Contains("CREATE TRIGGER trg_request_number_counters_set_updated_at", RequestCoreSql);
        Assert.Contains("EXECUTE FUNCTION set_updated_at();", RequestCoreSql);
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
