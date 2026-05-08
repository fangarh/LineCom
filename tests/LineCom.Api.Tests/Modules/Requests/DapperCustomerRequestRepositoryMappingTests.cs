using System.Reflection;
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class DapperCustomerRequestRepositoryMappingTests
{
    [Theory]
    [InlineData("InsertedRequestRecord")]
    [InlineData("RequestDetailRow")]
    [InlineData("RequestListRow")]
    [InlineData("RequestHistoryRow")]
    public void DapperRowTypes_UseDateTimeForPostgresTimestamptz(string nestedTypeName)
    {
        var nestedType = typeof(DapperCustomerRequestRepository).GetNestedType(
            nestedTypeName,
            BindingFlags.NonPublic);

        Assert.NotNull(nestedType);

        var createdAtProperty = nestedType.GetProperty("CreatedAt");

        Assert.NotNull(createdAtProperty);
        Assert.Equal(typeof(DateTime), createdAtProperty.PropertyType);
    }
}
