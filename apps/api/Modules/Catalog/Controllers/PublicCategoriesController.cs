using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

public sealed class PublicCategoriesController : PublicCatalogControllerBase
{
    private readonly IPublicCategoryQuery _categoryQuery;

    public PublicCategoriesController(IPublicCategoryQuery categoryQuery)
    {
        _categoryQuery = categoryQuery;
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(PublicCategoryTreeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicCategoryTreeResponse>> GetCategories(CancellationToken cancellationToken)
    {
        var response = await _categoryQuery.GetCategoryTreeAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("filters")]
    [ProducesResponseType(typeof(PublicCatalogFiltersDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicCatalogFiltersDto>> GetCatalogFilters(CancellationToken cancellationToken)
    {
        var response = await _categoryQuery.GetCatalogFiltersAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("categories/{slug}")]
    [ProducesResponseType(typeof(PublicCategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicCategoryDetailDto>> GetCategory(
        string slug,
        CancellationToken cancellationToken)
    {
        var response = await _categoryQuery.GetCategoryDetailAsync(slug, cancellationToken);

        return Ok(response);
    }

    [HttpGet("categories/{slug}/filters")]
    [ProducesResponseType(typeof(PublicCategoryFiltersDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicCategoryFiltersDto>> GetCategoryFilters(
        string slug,
        CancellationToken cancellationToken)
    {
        var response = await _categoryQuery.GetCategoryFiltersAsync(slug, cancellationToken);

        return Ok(response);
    }
}
