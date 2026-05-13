using LineCom.Api.Modules.Account.DTOs;
using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Account.Services;

public interface IAccountProfileService
{
    Task<AccountProfileDto> GetProfileAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<CurrentUserDto> UpdateProfileAsync(
        HttpContext httpContext,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountOrganizationDto> UpsertOrganizationAsync(
        HttpContext httpContext,
        UpsertAccountOrganizationRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        HttpContext httpContext,
        ChangeAccountPasswordRequest request,
        CancellationToken cancellationToken = default);
}
