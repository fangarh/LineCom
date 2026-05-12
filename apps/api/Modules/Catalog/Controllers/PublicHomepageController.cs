using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[ApiController]
[Route("api/public/homepage")]
public sealed class PublicHomepageController : ControllerBase
{
    private readonly IPublicHomepageQuery _query;

    public PublicHomepageController(IPublicHomepageQuery query)
    {
        _query = query;
    }

    [HttpGet("sections")]
    [ProducesResponseType(typeof(PublicHomepageSectionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicHomepageSectionsResponse>> GetSections(CancellationToken cancellationToken)
    {
        return Ok(await _query.GetSectionsAsync(cancellationToken));
    }
}
