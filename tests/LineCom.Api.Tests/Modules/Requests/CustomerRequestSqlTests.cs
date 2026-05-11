using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class CustomerRequestSqlTests
{
    [Fact]
    public void FindRequestHistory_ReturnsOnlyCustomerSafeEvents()
    {
        Assert.Contains("history.event_type IN ('created', 'status_changed')", CustomerRequestSql.FindRequestHistory);
        Assert.DoesNotContain("comment_added", CustomerRequestSql.FindRequestHistory);
    }
}
