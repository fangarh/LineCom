using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Requests.Controllers;

[Authorize]
[ApiController]
[Route("api/account/requests")]
public sealed class CustomerRequestsController : ControllerBase
{
    private readonly ICustomerRequestService _requestService;

    public CustomerRequestsController(ICustomerRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CustomerRequestListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CustomerRequestListResponse>> GetRequests(
        [FromQuery] CustomerRequestListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.GetRequestsAsync(
            HttpContext,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{number}")]
    [ProducesResponseType(typeof(CustomerRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerRequestDetailDto>> GetRequest(
        string number,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.GetRequestAsync(
            HttpContext,
            number,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(CustomerRequestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CustomerRequestDetailDto>> CreateRequest(
        CreateRequestCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.CreateRequestAsync(
            HttpContext,
            command,
            cancellationToken);

        return Created($"/api/account/requests/{Uri.EscapeDataString(response.Number)}", response);
    }
}
