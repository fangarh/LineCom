using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Auth.Services;

public interface IAuthCurrentUserService
{
    Task<AuthSessionDto> GetCurrentSessionAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
