using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/storage/diagnostics")]
public sealed class AdminStorageDiagnosticsController : ControllerBase
{
    private readonly IStorageDiagnosticsService diagnosticsService;

    public AdminStorageDiagnosticsController(IStorageDiagnosticsService diagnosticsService)
    {
        this.diagnosticsService = diagnosticsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminStorageDiagnosticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminStorageDiagnosticsResponse>> GetDiagnostics(
        [FromQuery] int? maxItems,
        CancellationToken cancellationToken)
    {
        return Ok(await diagnosticsService.GetDiagnosticsAsync(HttpContext, maxItems, cancellationToken));
    }
}
