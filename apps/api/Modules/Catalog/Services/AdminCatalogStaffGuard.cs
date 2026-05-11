using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogStaffGuard
{
    Task<CurrentUserDto> RequireStaffAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}

public sealed class AdminCatalogStaffGuard : IAdminCatalogStaffGuard
{
    private readonly IAuthCurrentUserService _currentUserService;

    public AdminCatalogStaffGuard(IAuthCurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserDto> RequireStaffAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var session = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        if (session.User.Role is "seller" or "admin")
        {
            return session.User;
        }

        throw AuthErrors.Forbidden();
    }
}
