using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class DapperPublicProductQueryTests
{
    [Theory]
    [InlineData("unknown", null)]
    [InlineData(null, "unknown")]
    public async Task GetProductsAsync_ThrowsInvalidFilter_BeforeOpeningConnection(
        string? availabilityStatus,
        string? saleUnit)
    {
        var queryService = new DapperPublicProductQuery(
            new ThrowingConnectionFactory(),
            new PublicCatalogReferenceData());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            queryService.GetProductsAsync(CreateQuery(availabilityStatus, saleUnit)));

        Assert.Equal("catalog.invalid_filter", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private static PublicProductListQuery CreateQuery(string? availabilityStatus, string? saleUnit)
    {
        return new PublicProductListQuery(
            CategorySlug: null,
            Page: 1,
            PageSize: 24,
            Sort: PublicProductSortKeys.Category,
            BrandSlug: null,
            availabilityStatus,
            saleUnit,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private sealed class ThrowingConnectionFactory : IDbConnectionFactory
    {
        public ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The database connection should not be opened for invalid public filters.");
        }
    }
}
