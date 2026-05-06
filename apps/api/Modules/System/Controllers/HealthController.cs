using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.System.Controllers;

[ApiController]
[Route("api/public/system")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse("ok", "LineCom.Api"));
    }
}

public sealed record HealthResponse(string Status, string Service);
