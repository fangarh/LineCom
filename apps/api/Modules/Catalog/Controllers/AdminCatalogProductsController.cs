using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/products")]
public sealed class AdminCatalogProductsController : ControllerBase
{
    private readonly IAdminCatalogProductService _productService;

    public AdminCatalogProductsController(IAdminCatalogProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminProductListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminProductListResponse>> GetProducts(
        [FromQuery] AdminProductListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductsAsync(
            HttpContext,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDetailDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductAsync(
            HttpContext,
            id,
            cancellationToken);

        return Ok(response);
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDetailDto>> CreateProduct(
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateProductAsync(
            HttpContext,
            command,
            cancellationToken);

        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDetailDto>> UpdateProduct(
        Guid id,
        UpsertAdminProductCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateProductAsync(
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
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(
            HttpContext,
            id,
            cancellationToken);

        return NoContent();
    }
}
