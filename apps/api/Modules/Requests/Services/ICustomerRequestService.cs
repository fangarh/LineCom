using LineCom.Api.Modules.Requests.DTOs;

namespace LineCom.Api.Modules.Requests.Services;

public interface ICustomerRequestService
{
    Task<CustomerRequestDetailDto> CreateRequestAsync(
        HttpContext httpContext,
        CreateRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<CustomerRequestListResponse> GetRequestsAsync(
        HttpContext httpContext,
        CustomerRequestListQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerRequestDetailDto> GetRequestAsync(
        HttpContext httpContext,
        string number,
        CancellationToken cancellationToken = default);
}
