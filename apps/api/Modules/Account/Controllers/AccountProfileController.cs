using LineCom.Api.Modules.Account.DTOs;
using LineCom.Api.Modules.Account.Services;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Account.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public sealed class AccountProfileController : ControllerBase
{
    private readonly IAccountProfileService _profileService;

    public AccountProfileController(IAccountProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<AccountProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        return Ok(await _profileService.GetProfileAsync(HttpContext, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("profile")]
    public async Task<ActionResult<CurrentUserDto>> UpdateProfile(
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _profileService.UpdateProfileAsync(HttpContext, request, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("organization")]
    public async Task<ActionResult<AccountOrganizationDto>> UpsertOrganization(
        UpsertAccountOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _profileService.UpsertOrganizationAsync(HttpContext, request, cancellationToken));
    }
}
