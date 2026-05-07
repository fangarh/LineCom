using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Auth.Services;

public interface ICustomerRegistrationService
{
    Task<CurrentUserDto> RegisterCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}
