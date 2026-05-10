using System.Reflection;
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class DapperAdminRequestRepositoryMappingTests
{
    [Theory]
    [InlineData("AdminRequestListRow")]
    [InlineData("AdminRequestDetailRow")]
    [InlineData("AdminRequestHistoryRow")]
    public void DapperRowTypes_UseDateTimeForPostgresTimestamptz(string nestedTypeName)
    {
        var nestedType = typeof(DapperAdminRequestRepository).GetNestedType(
            nestedTypeName,
            BindingFlags.NonPublic);

        Assert.NotNull(nestedType);

        var createdAtProperty = nestedType.GetProperty("CreatedAt");

        Assert.NotNull(createdAtProperty);
        Assert.Equal(typeof(DateTime), createdAtProperty.PropertyType);
    }
}
