using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/categories/{categoryId:guid}/attributes")]
public sealed class AdminCatalogAttributesController : ControllerBase
{
    private readonly IAdminCatalogAttributeService _attributeService;

    public AdminCatalogAttributesController(IAdminCatalogAttributeService attributeService)
    {
        _attributeService = attributeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminCategoryAttributesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminCategoryAttributesResponse>> GetAttributes(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var response = await _attributeService.GetAttributesAsync(
            HttpContext,
            categoryId,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminCategoryAttributeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminCategoryAttributeDto>> CreateAttribute(
        Guid categoryId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _attributeService.CreateAttributeAsync(
            HttpContext,
            categoryId,
            command,
            cancellationToken);

        return CreatedAtAction(nameof(GetAttributes), new { categoryId }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{attributeId:guid}")]
    [ProducesResponseType(typeof(AdminCategoryAttributeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminCategoryAttributeDto>> UpdateAttribute(
        Guid categoryId,
        Guid attributeId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _attributeService.UpdateAttributeAsync(
            HttpContext,
            categoryId,
            attributeId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpDelete("{attributeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAttribute(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken)
    {
        await _attributeService.DeleteAttributeAsync(
            HttpContext,
            categoryId,
            attributeId,
            cancellationToken);

        return NoContent();
    }

    [RequireCsrfToken]
    [HttpPost("{attributeId:guid}/options")]
    [ProducesResponseType(typeof(AdminAttributeOptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminAttributeOptionDto>> CreateOption(
        Guid categoryId,
        Guid attributeId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _attributeService.CreateOptionAsync(
            HttpContext,
            categoryId,
            attributeId,
            command,
            cancellationToken);

        return CreatedAtAction(nameof(GetAttributes), new { categoryId }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{attributeId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(typeof(AdminAttributeOptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminAttributeOptionDto>> UpdateOption(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _attributeService.UpdateOptionAsync(
            HttpContext,
            categoryId,
            attributeId,
            optionId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpDelete("{attributeId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteOption(
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        await _attributeService.DeleteOptionAsync(
            HttpContext,
            categoryId,
            attributeId,
            optionId,
            cancellationToken);

        return NoContent();
    }

    [RequireCsrfToken]
    [HttpPost("inherit-from-parent")]
    [ProducesResponseType(typeof(InheritAdminCategoryAttributesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InheritAdminCategoryAttributesResponse>> InheritFromParent(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var response = await _attributeService.InheritFromParentAsync(
            HttpContext,
            categoryId,
            cancellationToken);

        return Ok(response);
    }
}
