namespace LineCom.Api.Modules.Requests.Services;

public sealed record GeneratedRequestNumber(
    int Year,
    int Sequence,
    string Number);

public interface IRequestNumberGenerator
{
    Task<GeneratedRequestNumber> GenerateNextAsync(
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default);
}
