using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Modules.Requests.Services;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class RequestNumberGeneratorTests
{
    [Theory]
    [InlineData(2026, 1, "ЗК26-0001")]
    [InlineData(2026, 12, "ЗК26-0012")]
    [InlineData(2030, 10001, "ЗК30-10001")]
    public async Task GenerateNextAsync_FormatsYearAndSequence(
        int year,
        int sequence,
        string expectedNumber)
    {
        var repository = new CapturingRequestNumberRepository(sequence);
        var generator = new RequestNumberGenerator(repository);
        var requestedAt = new DateTimeOffset(year, 5, 7, 10, 30, 0, TimeSpan.Zero);

        var result = await generator.GenerateNextAsync(requestedAt);

        Assert.Equal(year, result.Year);
        Assert.Equal(sequence, result.Sequence);
        Assert.Equal(expectedNumber, result.Number);
        Assert.Equal(year, repository.CapturedYear);
    }

    private sealed class CapturingRequestNumberRepository : IRequestNumberRepository
    {
        private readonly int _sequence;

        public CapturingRequestNumberRepository(int sequence)
        {
            _sequence = sequence;
        }

        public int? CapturedYear { get; private set; }

        public Task<int> GetNextSequenceAsync(
            int year,
            CancellationToken cancellationToken = default)
        {
            CapturedYear = year;

            return Task.FromResult(_sequence);
        }
    }
}
