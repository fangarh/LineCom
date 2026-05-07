namespace LineCom.Api.Modules.Requests.Repositories;

public interface IRequestNumberRepository
{
    Task<int> GetNextSequenceAsync(
        int year,
        CancellationToken cancellationToken = default);
}
