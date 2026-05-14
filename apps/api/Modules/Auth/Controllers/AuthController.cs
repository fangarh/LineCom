using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LineCom.Api.Modules.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICustomerRegistrationService _registrationService;
    private readonly ICustomerLoginService _loginService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IAuthCurrentUserService _currentUserService;

    public AuthController(
        ICustomerRegistrationService registrationService,
        ICustomerLoginService loginService,
        IAuthSessionService authSessionService,
        IAuthCurrentUserService currentUserService)
    {
        _registrationService = registrationService;
        _loginService = loginService;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
    }

    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    [HttpPost("register")]
    public async Task<ActionResult<AuthSessionDto>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _registrationService.RegisterCustomerAsync(request, cancellationToken);
        var session = await _authSessionService.SignInAsync(HttpContext, user, cancellationToken);

        return Created("/api/auth/me", session);
    }

    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthSessionDto>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _loginService.LoginAsync(request, cancellationToken);
        var session = await _authSessionService.SignInAsync(HttpContext, user, cancellationToken);

        return Ok(session);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthSessionDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _currentUserService.GetCurrentSessionAsync(HttpContext, cancellationToken));
    }

    [Authorize]
    [RequireCsrfToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }
}
