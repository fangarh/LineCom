using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;

namespace LineCom.Api.Modules.Auth.Services;

public sealed class CustomerLoginService : ICustomerLoginService
{
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CustomerLoginService(
        IUserLoginRepository userLoginRepository,
        IPasswordHasher passwordHasher)
    {
        _userLoginRepository = userLoginRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<CurrentUserDto> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var login = AuthInputNormalizer.RequiredText(request.Login);
        var password = AuthInputNormalizer.RequiredText(request.Password);
        if (login is null || password is null)
        {
            throw AuthErrors.InvalidRequest();
        }

        var (email, phone) = AuthInputNormalizer.LoginContact(login);
        var user = await _userLoginRepository.FindByEmailOrPhoneAsync(email, phone, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
            throw AuthErrors.InvalidCredentials();
        }

        if (!user.IsActive)
        {
            throw AuthErrors.UserInactive();
        }

        return new CurrentUserDto(user.Id, user.Name, user.Email, user.Phone, user.Role);
    }
}
