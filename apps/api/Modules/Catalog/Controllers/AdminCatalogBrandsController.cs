using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/brands")]
public sealed class AdminCatalogBrandsController : ControllerBase
{
    private readonly IAdminCatalogBrandService _brandService;

    public AdminCatalogBrandsController(IAdminCatalogBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminBrandListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminBrandListResponse>> GetBrands(
        [FromQuery] AdminBrandListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _brandService.GetBrandsAsync(
            HttpContext,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminBrandDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminBrandDetailDto>> GetBrand(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _brandService.GetBrandAsync(
            HttpContext,
            id,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminBrandDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminBrandDetailDto>> CreateBrand(
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _brandService.CreateBrandAsync(
            HttpContext,
            command,
            cancellationToken);

        return CreatedAtAction(nameof(GetBrand), new { id = created.Id }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminBrandDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminBrandDetailDto>> UpdateBrand(
        Guid id,
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _brandService.UpdateBrandAsync(
            HttpContext,
            id,
            command,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBrand(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _brandService.DeleteBrandAsync(
            HttpContext,
            id,
            cancellationToken);

        return NoContent();
    }
}
