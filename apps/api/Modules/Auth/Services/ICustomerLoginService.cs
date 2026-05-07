using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Auth.Services;

public interface ICustomerLoginService
{
    Task<CurrentUserDto> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
