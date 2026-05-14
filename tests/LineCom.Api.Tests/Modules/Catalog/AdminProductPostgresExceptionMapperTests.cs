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

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.RaiseException)]
    public void IsInvalidRequest_IncludesProductRequestConstraintViolations(string sqlState)
    {
        var exception = CreatePostgresException(sqlState, constraintName: null);

        Assert.True(AdminProductPostgresExceptionMapper.IsInvalidRequest(exception));
    }

    [Fact]
    public void IsInvalidRequest_IgnoresUniqueViolations()
    {
        var exception = CreatePostgresException(PostgresErrorCodes.UniqueViolation, "ux_products_sku");

        Assert.False(AdminProductPostgresExceptionMapper.IsInvalidRequest(exception));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.RaiseException)]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    public void IsInvalidAttributeUpdate_IncludesProductAttributeConstraintViolations(string sqlState)
    {
        var exception = CreatePostgresException(sqlState, constraintName: null);

        Assert.True(AdminProductPostgresExceptionMapper.IsInvalidAttributeUpdate(exception));
    }

    [Fact]
    public void IsInvalidAttributeUpdate_IgnoresUnrelatedViolations()
    {
        var exception = CreatePostgresException(PostgresErrorCodes.NotNullViolation, constraintName: null);

        Assert.False(AdminProductPostgresExceptionMapper.IsInvalidAttributeUpdate(exception));
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
