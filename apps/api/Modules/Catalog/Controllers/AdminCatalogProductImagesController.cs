using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/products/{productId:guid}/images")]
public sealed class AdminCatalogProductImagesController : ControllerBase
{
    private readonly IAdminCatalogImageService _imageService;

    public AdminCatalogProductImagesController(IAdminCatalogImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> GetProductImages(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.GetProductImagesAsync(HttpContext, productId, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> UploadProductImages(
        Guid productId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UploadProductImagesAsync(HttpContext, productId, files, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("order")]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> UpdateProductImageOrder(
        Guid productId,
        UpdateAdminProductImageOrderCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UpdateProductImageOrderAsync(HttpContext, productId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{imageId:guid}")]
    [ProducesResponseType(typeof(AdminProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImageDto>> UpdateProductImage(
        Guid productId,
        Guid imageId,
        UpdateAdminProductImageCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UpdateProductImageAsync(HttpContext, productId, imageId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{imageId:guid}/main")]
    [ProducesResponseType(typeof(AdminProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImageDto>> SetMainProductImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.SetMainProductImageAsync(HttpContext, productId, imageId, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await _imageService.DeleteProductImageAsync(HttpContext, productId, imageId, cancellationToken);
        return NoContent();
    }
}
