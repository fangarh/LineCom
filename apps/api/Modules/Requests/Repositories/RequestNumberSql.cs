namespace LineCom.Api.Modules.Requests.Repositories;

internal static class RequestNumberSql
{
    public const string GetNextSequence = """
        INSERT INTO request_number_counters (
            year,
            next_sequence
        )
        VALUES (
            @Year,
            2
        )
        ON CONFLICT (year) DO UPDATE
        SET next_sequence = request_number_counters.next_sequence + 1
        RETURNING next_sequence - 1;
        """;
}
