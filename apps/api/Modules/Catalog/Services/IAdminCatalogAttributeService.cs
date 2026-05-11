using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogAttributeService
{
    Task<AdminCategoryAttributesResponse> GetAttributesAsync(
        HttpContext httpContext,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeDto> CreateAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryAttributeDto> UpdateAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        UpsertAdminCategoryAttributeCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAttributeAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default);

    Task<AdminAttributeOptionDto> CreateOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminAttributeOptionDto> UpdateOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        UpsertAdminAttributeOptionCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteOptionAsync(
        HttpContext httpContext,
        Guid categoryId,
        Guid attributeId,
        Guid optionId,
        CancellationToken cancellationToken = default);

    Task<InheritAdminCategoryAttributesResponse> InheritFromParentAsync(
        HttpContext httpContext,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
