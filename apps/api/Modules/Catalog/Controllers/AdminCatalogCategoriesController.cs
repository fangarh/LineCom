using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/categories")]
public sealed class AdminCatalogCategoriesController : ControllerBase
{
    private readonly IAdminCatalogCategoryService _categoryService;

    public AdminCatalogCategoriesController(IAdminCatalogCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminCategoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminCategoryListResponse>> GetCategories(
        [FromQuery] AdminCategoryListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetCategoriesAsync(
            HttpContext,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminCategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCategoryDetailDto>> GetCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetCategoryAsync(
            HttpContext,
            id,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminCategoryDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminCategoryDetailDto>> CreateCategory(
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateCategoryAsync(
            HttpContext,
            command,
            cancellationToken);

        return CreatedAtAction(nameof(GetCategory), new { id = created.Id }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminCategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminCategoryDetailDto>> UpdateCategory(
        Guid id,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.UpdateCategoryAsync(
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
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(
            HttpContext,
            id,
            cancellationToken);

        return NoContent();
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/move")]
    [ProducesResponseType(typeof(AdminCategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminCategoryDetailDto>> MoveCategory(
        Guid id,
        MoveAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.MoveCategoryAsync(
            HttpContext,
            id,
            command,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/sort")]
    [ProducesResponseType(typeof(AdminCategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCategoryDetailDto>> SortCategory(
        Guid id,
        SortAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _categoryService.SortCategoryAsync(
            HttpContext,
            id,
            command,
            cancellationToken);

        return Ok(response);
    }
}
