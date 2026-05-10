using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class AdminRequestSqlTests
{
    [Fact]
    public void FindRequests_FiltersWithoutUserScope()
    {
        Assert.Contains("FROM requests request", AdminRequestSql.FindRequests);
        Assert.DoesNotContain("request.user_id = @UserId", AdminRequestSql.FindRequests);
        Assert.Contains("request.status = @Status", AdminRequestSql.FindRequests);
        Assert.Contains("request.number ILIKE", AdminRequestSql.FindRequests);
        Assert.Contains("request.customer_email::text", AdminRequestSql.FindRequests);
        Assert.Contains("request.organization_inn", AdminRequestSql.FindRequests);
    }

    [Fact]
    public void UpdateStatus_IsTransactionalFriendlyAndIdempotent()
    {
        Assert.Contains("FOR UPDATE", AdminRequestSql.FindRequestForUpdate);
        Assert.Contains("UPDATE requests", AdminRequestSql.UpdateStatus);
        Assert.Contains("WHERE id = @RequestId", AdminRequestSql.UpdateStatus);
        Assert.Contains("INSERT INTO request_history", AdminRequestSql.InsertStatusChangedHistory);
        Assert.Contains("status_changed", AdminRequestSql.InsertStatusChangedHistory);
    }

    [Fact]
    public void UpdateInternalComment_WritesCurrentCommentAndHistory()
    {
        Assert.Contains("internal_comment = @InternalComment", AdminRequestSql.UpdateInternalComment);
        Assert.Contains("comment_added", AdminRequestSql.InsertInternalCommentHistory);
        Assert.Contains("actor_user_id", AdminRequestSql.InsertInternalCommentHistory);
    }
}
