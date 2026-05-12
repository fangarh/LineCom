using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminHomepageService
{
    Task<AdminHomepageSectionsResponse> GetSectionsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionDto> UpdateSectionAsync(
        HttpContext httpContext,
        Guid id,
        UpdateAdminHomepageSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionItemDto> CreateItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        CreateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionsResponse> UpdateItemOrderAsync(
        HttpContext httpContext,
        Guid sectionId,
        UpdateAdminHomepageSectionItemOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminHomepageSectionItemDto> UpdateItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        Guid itemId,
        UpdateAdminHomepageSectionItemCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteItemAsync(
        HttpContext httpContext,
        Guid sectionId,
        Guid itemId,
        CancellationToken cancellationToken = default);
}
