using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Modules.Requests.Services;

public sealed class RequestNumberGenerator : IRequestNumberGenerator
{
    private readonly IRequestNumberRepository _numberRepository;

    public RequestNumberGenerator(IRequestNumberRepository numberRepository)
    {
        _numberRepository = numberRepository;
    }

    public async Task<GeneratedRequestNumber> GenerateNextAsync(
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default)
    {
        var year = requestedAt.Year;
        var sequence = await _numberRepository.GetNextSequenceAsync(year, cancellationToken);
        var number = $"ЗК{year % 100:00}-{sequence:0000}";

        return new GeneratedRequestNumber(year, sequence, number);
    }
}
