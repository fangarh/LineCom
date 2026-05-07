namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class ProductImageSharedFilesMigrationTests
{
    private static readonly string MigrationSql = ReadMigration("005_product_image_shared_files.sql");

    [Fact]
    public void ProductImageSharedFiles_RemovesStoredFileUniqueness()
    {
        Assert.Contains("DROP INDEX IF EXISTS ux_product_images_stored_file_id;", MigrationSql);
    }

    [Fact]
    public void ProductImageSharedFiles_AddsProductFilePairUniqueness()
    {
        var normalizedSql = MigrationSql.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains(
            "CREATE UNIQUE INDEX IF NOT EXISTS ux_product_images_product_id_stored_file_id\n    ON product_images (product_id, stored_file_id);",
            normalizedSql);
    }

    [Fact]
    public void ProductImageSharedFiles_DoesNotRemoveSingleMainImageRule()
    {
        Assert.DoesNotContain("DROP INDEX IF EXISTS ux_product_images_single_main", MigrationSql);
        Assert.DoesNotContain("DROP INDEX ux_product_images_single_main", MigrationSql);
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
