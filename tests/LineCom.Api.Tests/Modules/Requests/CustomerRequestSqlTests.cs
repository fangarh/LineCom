using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class CustomerRequestSqlTests
{
    [Fact]
    public void FindProductSnapshots_SelectsOnlyActivePublishedProducts()
    {
        Assert.Contains("product.id = ANY(@ProductIds)", CustomerRequestSql.FindProductSnapshots);
        Assert.Contains("product.is_active = TRUE", CustomerRequestSql.FindProductSnapshots);
        Assert.Contains("product.publish_status = 'published'", CustomerRequestSql.FindProductSnapshots);
        Assert.Contains("category.is_active = TRUE", CustomerRequestSql.FindProductSnapshots);
    }

    [Fact]
    public void FindRequestHistory_ReturnsOnlyCustomerSafeEvents()
    {
        Assert.Contains("history.event_type IN ('created', 'status_changed')", CustomerRequestSql.FindRequestHistory);
        Assert.DoesNotContain("comment_added", CustomerRequestSql.FindRequestHistory);
    }

    [Fact]
    public void FindRequestHistory_MapsCreatedEventToRussianMessage()
    {
        Assert.Contains("WHEN 'created' THEN 'Заявка создана.'", CustomerRequestSql.FindRequestHistory);
    }
}
