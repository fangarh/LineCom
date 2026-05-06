using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

public sealed class PublicProductsController : PublicCatalogControllerBase
{
    private readonly IPublicProductQuery _productQuery;

    public PublicProductsController(IPublicProductQuery productQuery)
    {
        _productQuery = productQuery;
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(PublicProductListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicProductListResponse>> GetProducts(CancellationToken cancellationToken)
    {
        var query = PublicProductListQueryParser.Parse(Request.Query);
        var response = await _productQuery.GetProductsAsync(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("products/{slug}")]
    [ProducesResponseType(typeof(PublicProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicProductDetailDto>> GetProduct(
        string slug,
        CancellationToken cancellationToken)
    {
        var response = await _productQuery.GetProductDetailAsync(slug, cancellationToken);

        return Ok(response);
    }
}
