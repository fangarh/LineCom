using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using LineCom.Api.Tests.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

[Collection(PostgresMigrationCollection.Name)]
public sealed class DapperPublicProductQueryDatabaseTests
{
    private readonly PostgresMigrationFixture _fixture;

    public DapperPublicProductQueryDatabaseTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsPublishedProductsOnly()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = CreateQuery(dataSource);
        var response = await query.GetProductsAsync(new PublicProductListQuery(
            "cable",
            1,
            24,
            PublicProductSortKeys.Category,
            null,
            null,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)));

        var item = Assert.Single(response.Items);
        Assert.Equal("u-utp-cat-5e", item.Slug);
        Assert.Equal("/storage/products/cable.jpg", item.MainImage?.Url);
        Assert.Equal(1, response.TotalItems);
    }

    [Fact]
    public async Task GetProductsAsync_FiltersBySelectAttributeSlug()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = CreateQuery(dataSource);
        var response = await query.GetProductsAsync(new PublicProductListQuery(
            "cable",
            1,
            24,
            PublicProductSortKeys.Category,
            null,
            null,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["conductor-material"] = "cu"
            }));

        Assert.Equal("u-utp-cat-5e", Assert.Single(response.Items).Slug);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsSeoImagesAttributesAndBreadcrumbs()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = CreateQuery(dataSource);
        var response = await query.GetProductDetailAsync("u-utp-cat-5e");

        Assert.Equal("u-utp-cat-5e", response.Slug);
        Assert.Equal("/catalog/products/u-utp-cat-5e", response.Seo.CanonicalPath);
        Assert.Equal("/storage/products/cable.jpg", Assert.Single(response.Images).Url);
        Assert.Equal("conductor-material", Assert.Single(response.Attributes).Code);
        Assert.Equal("u-utp-cat-5e", response.Breadcrumbs[^1].Slug);
    }

    [Fact]
    public async Task GetProductDetailAsync_ThrowsNotFoundForDraftProduct()
    {
        if (!_fixture.IsConfigured) return;

        await using var dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await PublicCatalogPostgresTestData.SeedAsync(connection);

        var query = CreateQuery(dataSource);
        var exception = await Assert.ThrowsAsync<ApiException>(() => query.GetProductDetailAsync("draft-product"));

        Assert.Equal("catalog.product_not_found", exception.Code);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    private static DapperPublicProductQuery CreateQuery(NpgsqlDataSource dataSource)
    {
        return new DapperPublicProductQuery(
            new NpgsqlConnectionFactory(dataSource),
            new PublicCatalogReferenceData());
    }
}
