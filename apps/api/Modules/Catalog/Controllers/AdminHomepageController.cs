using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/homepage/sections")]
public sealed class AdminHomepageController : ControllerBase
{
    private readonly IAdminHomepageService _service;

    public AdminHomepageController(IAdminHomepageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AdminHomepageSectionsResponse>> GetSections(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetSectionsAsync(HttpContext, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminHomepageSectionDto>> UpdateSection(
        Guid id,
        UpdateAdminHomepageSectionCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateSectionAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<AdminHomepageSectionItemDto>> CreateItem(
        Guid id,
        CreateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateItemAsync(HttpContext, id, command, cancellationToken);

        return CreatedAtAction(nameof(GetSections), created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/items/order")]
    public async Task<ActionResult<AdminHomepageSectionsResponse>> UpdateItemOrder(
        Guid id,
        UpdateAdminHomepageSectionItemOrderCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateItemOrderAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<AdminHomepageSectionItemDto>> UpdateItem(
        Guid id,
        Guid itemId,
        UpdateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateItemAsync(HttpContext, id, itemId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await _service.DeleteItemAsync(HttpContext, id, itemId, cancellationToken);

        return NoContent();
    }
}
