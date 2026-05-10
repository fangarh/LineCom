namespace LineCom.Api.Modules.Requests.Repositories;

public sealed class DapperAdminRequestRepository : IAdminRequestRepository
{
    public Task<AdminRequestListRecordResponse> GetRequestsAsync(
        AdminRequestReadListQuery query,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<AdminRequestDetailRecord?> GetRequestAsync(
        string number,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<AdminRequestDetailRecord?> UpdateStatusAsync(
        AdminRequestStatusUpdate update,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(
        AdminRequestInternalCommentUpdate update,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
