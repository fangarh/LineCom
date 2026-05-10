using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Requests.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/requests")]
public sealed class AdminRequestsController : ControllerBase
{
    private readonly IAdminRequestService _requestService;

    public AdminRequestsController(IAdminRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminRequestListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRequestListResponse>> GetRequests(
        [FromQuery] AdminRequestListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.GetRequestsAsync(
            HttpContext,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{number}")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDetailDto>> GetRequest(
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
    [HttpPatch("{number}/status")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDetailDto>> UpdateStatus(
        string number,
        UpdateAdminRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.UpdateStatusAsync(
            HttpContext,
            number,
            command,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPut("{number}/internal-comment")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDetailDto>> UpdateInternalComment(
        string number,
        UpdateAdminRequestInternalCommentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _requestService.UpdateInternalCommentAsync(
            HttpContext,
            number,
            command,
            cancellationToken);

        return Ok(response);
    }
}
