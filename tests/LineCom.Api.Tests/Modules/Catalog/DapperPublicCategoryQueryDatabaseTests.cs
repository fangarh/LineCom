using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Tests.Infrastructure.Database;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class DapperPublicCategoryQueryDatabaseTests
{
    private readonly PostgresMigrationFixture _fixture;

    public DapperPublicCategoryQueryDatabaseTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCategoryTreeAsync_ReturnsActiveCategoriesOnly()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = new DapperPublicCategoryQuery(new NpgsqlConnectionFactory(dataSource));

        var response = await query.GetCategoryTreeAsync();

        var category = Assert.Single(response.Items);
        Assert.Equal("cable", category.Slug);
        Assert.DoesNotContain(response.Items, item => item.Slug == "hidden-category");
    }

    [Fact]
    public async Task GetCategoryFiltersAsync_ReturnsFilterOptions()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = new DapperPublicCategoryQuery(new NpgsqlConnectionFactory(dataSource));

        var response = await query.GetCategoryFiltersAsync("cable");

        var filter = Assert.Single(response.Filters);
        Assert.Equal("conductor-material", filter.Code);
        Assert.Equal("cu", Assert.Single(filter.Options).Slug);
    }
}
