namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class AuthRequestCoreMigrationTests
{
    private static readonly string AuthRequestCoreSql = ReadMigration("003_auth_users_organizations.sql");

    [Theory]
    [InlineData("users")]
    [InlineData("organizations")]
    public void AuthRequestCore_CreatesExpectedReleaseTables(string tableName)
    {
        Assert.Contains($"CREATE TABLE {tableName} (", AuthRequestCoreSql);
    }

    [Theory]
    [InlineData("ck_users_role CHECK (role IN ('customer', 'seller', 'admin'))")]
    [InlineData("ck_users_contact_required CHECK (email IS NOT NULL OR phone IS NOT NULL)")]
    public void AuthRequestCore_ConstrainsUserReleaseValues(string expectedConstraint)
    {
        Assert.Contains(expectedConstraint, AuthRequestCoreSql);
    }

    [Theory]
    [InlineData("CREATE UNIQUE INDEX ux_users_email ON users (email) WHERE email IS NOT NULL;")]
    [InlineData("CREATE UNIQUE INDEX ux_users_phone ON users (phone) WHERE phone IS NOT NULL;")]
    [InlineData("CREATE UNIQUE INDEX ux_organizations_user_id ON organizations (user_id);")]
    public void AuthRequestCore_DefinesIdentityAndCardinalityIndexes(string expectedIndex)
    {
        Assert.Contains(expectedIndex, AuthRequestCoreSql);
    }

    [Fact]
    public void AuthRequestCore_StoresOnlyPasswordHash()
    {
        Assert.Contains("password_hash text NOT NULL", AuthRequestCoreSql);
        Assert.Contains("ck_users_password_hash_not_blank CHECK (btrim(password_hash) <> '')", AuthRequestCoreSql);
        Assert.DoesNotContain("password text", AuthRequestCoreSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthRequestCore_LinksOrganizationToUser()
    {
        Assert.Contains("user_id uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE", AuthRequestCoreSql);
    }

    [Fact]
    public void AuthRequestCore_DefinesUpdatedAtTriggers()
    {
        Assert.Contains("CREATE TRIGGER trg_users_set_updated_at", AuthRequestCoreSql);
        Assert.Contains("CREATE TRIGGER trg_organizations_set_updated_at", AuthRequestCoreSql);
        Assert.Contains("EXECUTE FUNCTION set_updated_at();", AuthRequestCoreSql);
    }

    [Fact]
    public void AuthRequestCore_DoesNotCreateRequestTables()
    {
        Assert.DoesNotContain("CREATE TABLE requests", AuthRequestCoreSql);
        Assert.DoesNotContain("CREATE TABLE request_items", AuthRequestCoreSql);
        Assert.DoesNotContain("CREATE TABLE request_history", AuthRequestCoreSql);
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
