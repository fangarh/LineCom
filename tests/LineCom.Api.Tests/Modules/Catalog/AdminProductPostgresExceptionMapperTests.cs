using LineCom.Api.Modules.Catalog.Repositories;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminProductPostgresExceptionMapperTests
{
    [Theory]
    [InlineData("ux_products_sku", "sku")]
    [InlineData("ux_products_external_id", "external_id")]
    [InlineData("ux_products_slug", "slug")]
    [InlineData(null, "slug")]
    public void TryGetDuplicateField_MapsKnownProductIdentityConstraints(string? constraintName, string expectedField)
    {
        var exception = CreatePostgresException(PostgresErrorCodes.UniqueViolation, constraintName);

        var mapped = AdminProductPostgresExceptionMapper.TryGetDuplicateField(exception, out var field);

        Assert.True(mapped);
        Assert.Equal(expectedField, field);
    }

    [Fact]
    public void TryGetDuplicateField_IgnoresNonUniqueViolations()
    {
        var exception = CreatePostgresException(PostgresErrorCodes.ForeignKeyViolation, "ux_products_sku");

        var mapped = AdminProductPostgresExceptionMapper.TryGetDuplicateField(exception, out var field);

        Assert.False(mapped);
        Assert.Equal(string.Empty, field);
    }

    private static PostgresException CreatePostgresException(string sqlState, string? constraintName)
    {
        return new PostgresException(
            "message",
            "ERROR",
            "ERROR",
            sqlState,
            constraintName: constraintName);
    }
}
