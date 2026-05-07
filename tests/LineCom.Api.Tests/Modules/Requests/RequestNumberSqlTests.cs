using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class RequestNumberSqlTests
{
    [Fact]
    public void GetNextSequence_UsesParameterizedYearAndAtomicUpsert()
    {
        Assert.Contains("year", RequestNumberSql.GetNextSequence);
        Assert.Contains("@Year", RequestNumberSql.GetNextSequence);
        Assert.Contains("ON CONFLICT (year) DO UPDATE", RequestNumberSql.GetNextSequence);
        Assert.Contains("request_number_counters.next_sequence + 1", RequestNumberSql.GetNextSequence);
        Assert.Contains("RETURNING next_sequence - 1", RequestNumberSql.GetNextSequence);
    }
}
