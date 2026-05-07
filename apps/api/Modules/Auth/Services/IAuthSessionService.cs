using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Auth.Services;

public interface IAuthSessionService
{
    Task<AuthSessionDto> SignInAsync(
        HttpContext httpContext,
        CurrentUserDto user,
        CancellationToken cancellationToken = default);
}
