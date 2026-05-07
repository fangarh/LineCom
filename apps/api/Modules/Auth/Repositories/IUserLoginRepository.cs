namespace LineCom.Api.Modules.Auth.Repositories;

public sealed record LoginUser(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Role,
    string PasswordHash,
    bool IsActive);

public sealed record CurrentAuthUser(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Role,
    bool IsActive);

public interface IUserLoginRepository
{
    Task<LoginUser?> FindByEmailOrPhoneAsync(
        string? email,
        string? phone,
        CancellationToken cancellationToken = default);

    Task<CurrentAuthUser?> FindCurrentUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
