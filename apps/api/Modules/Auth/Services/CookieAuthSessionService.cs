using System.Security.Claims;
using System.Security.Cryptography;
using LineCom.Api.Modules.Auth.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;

namespace LineCom.Api.Modules.Auth.Services;

public sealed class CookieAuthSessionService : IAuthSessionService
{
    public const string CsrfClaimType = "linecom_csrf";

    public async Task<AuthSessionDto> SignInAsync(
        HttpContext httpContext,
        CurrentUserDto user,
        CancellationToken cancellationToken = default)
    {
        var csrfToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role),
            new(CsrfClaimType, csrfToken)
        };

        if (user.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (user.Phone is not null)
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return new AuthSessionDto(user, csrfToken);
    }
}
