using LineCom.Api.Modules.Requests.DTOs;

namespace LineCom.Api.Modules.Requests.Services;

public interface IAdminRequestService
{
    Task<AdminRequestListResponse> GetRequestsAsync(
        HttpContext httpContext,
        AdminRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailDto> GetRequestAsync(
        HttpContext httpContext,
        string number,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailDto> UpdateStatusAsync(
        HttpContext httpContext,
        string number,
        UpdateAdminRequestStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminRequestDetailDto> UpdateInternalCommentAsync(
        HttpContext httpContext,
        string number,
        UpdateAdminRequestInternalCommentCommand command,
        CancellationToken cancellationToken = default);
}
