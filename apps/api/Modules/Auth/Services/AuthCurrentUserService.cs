using System.Security.Claims;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;

namespace LineCom.Api.Modules.Auth.Services;

public sealed class AuthCurrentUserService : IAuthCurrentUserService
{
    private readonly IUserLoginRepository _userLoginRepository;

    public AuthCurrentUserService(IUserLoginRepository userLoginRepository)
    {
        _userLoginRepository = userLoginRepository;
    }

    public async Task<AuthSessionDto> GetCurrentSessionAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var principal = httpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw AuthErrors.Unauthorized();
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var csrfToken = principal.FindFirstValue(CookieAuthSessionService.CsrfClaimType);
        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(csrfToken))
        {
            throw AuthErrors.Unauthorized();
        }

        var user = await _userLoginRepository.FindCurrentUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw AuthErrors.Unauthorized();
        }

        if (!user.IsActive)
        {
            throw AuthErrors.UserInactive();
        }

        return new AuthSessionDto(
            new CurrentUserDto(user.Id, user.Name, user.Email, user.Phone, user.Role),
            csrfToken);
    }
}
