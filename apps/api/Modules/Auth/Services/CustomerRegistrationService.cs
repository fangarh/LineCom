using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;

namespace LineCom.Api.Modules.Auth.Services;

public sealed class CustomerRegistrationService : ICustomerRegistrationService
{
    private readonly IUserRegistrationRepository _userRegistrationRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CustomerRegistrationService(
        IUserRegistrationRepository userRegistrationRepository,
        IPasswordHasher passwordHasher)
    {
        _userRegistrationRepository = userRegistrationRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<CurrentUserDto> RegisterCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = AuthInputNormalizer.RequiredText(request.Name);
        var email = AuthInputNormalizer.Email(request.Email);
        var phone = AuthInputNormalizer.Phone(request.Phone);
        var password = request.Password;

        if (name is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        if (email is null && phone is null)
        {
            throw AuthErrors.InvalidContact();
        }

        if (!AuthPasswordPolicy.IsValidPassword(password))
        {
            throw AuthErrors.InvalidPassword();
        }

        var registration = new NewUserRegistration(
            name,
            email,
            phone,
            _passwordHasher.HashPassword(password!),
            "customer",
            IsActive: true);

        try
        {
            var user = await _userRegistrationRepository.CreateCustomerAsync(registration, cancellationToken);
            return new CurrentUserDto(user.Id, user.Name, user.Email, user.Phone, user.Role);
        }
        catch (DuplicateUserContactException)
        {
            throw AuthErrors.UserAlreadyExists();
        }
    }
}
