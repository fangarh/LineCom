using LineCom.Api.Modules.Account.DTOs;
using LineCom.Api.Modules.Account.Repositories;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;

namespace LineCom.Api.Modules.Account.Services;

public sealed class AccountProfileService : IAccountProfileService
{
    private readonly IAuthCurrentUserService _currentUserService;
    private readonly IAccountProfileRepository _profileRepository;

    public AccountProfileService(
        IAuthCurrentUserService currentUserService,
        IAccountProfileRepository profileRepository)
    {
        _currentUserService = currentUserService;
        _profileRepository = profileRepository;
    }

    public async Task<AccountProfileDto> GetProfileAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var organization = await _profileRepository.FindOrganizationAsync(
            currentSession.User.Id,
            cancellationToken);

        return new AccountProfileDto(
            currentSession.User,
            organization is null ? null : ToDto(organization));
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(
        HttpContext httpContext,
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var name = AuthInputNormalizer.RequiredText(request.Name);
        var email = AuthInputNormalizer.Email(request.Email);
        var phone = AuthInputNormalizer.Phone(request.Phone);

        if (name is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        if (email is null && phone is null)
        {
            throw AuthErrors.InvalidContact();
        }

        try
        {
            return await _profileRepository.UpdateProfileAsync(
                currentSession.User.Id,
                new AccountProfileUpdate(name, email, phone),
                cancellationToken);
        }
        catch (DuplicateUserContactException)
        {
            throw AuthErrors.UserAlreadyExists();
        }
    }

    public async Task<AccountOrganizationDto> UpsertOrganizationAsync(
        HttpContext httpContext,
        UpsertAccountOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSession = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        var name = AuthInputNormalizer.RequiredText(request.Name);
        if (name is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        var upsert = new AccountOrganizationUpsert(
            name,
            AuthInputNormalizer.RequiredText(request.Inn),
            AuthInputNormalizer.RequiredText(request.ContactPerson),
            AuthInputNormalizer.Phone(request.Phone),
            AuthInputNormalizer.Email(request.Email),
            AuthInputNormalizer.RequiredText(request.Comment));

        var organization = await _profileRepository.UpsertOrganizationAsync(
            currentSession.User.Id,
            upsert,
            cancellationToken);

        return ToDto(organization);
    }

    private static AccountOrganizationDto ToDto(AccountOrganizationRecord organization)
    {
        return new AccountOrganizationDto(
            organization.Name,
            organization.Inn,
            organization.ContactPerson,
            organization.Phone,
            organization.Email,
            organization.Comment);
    }
}
