namespace LineCom.Api.Modules.Auth.Repositories;

public sealed record NewUserRegistration(
    string Name,
    string? Email,
    string? Phone,
    string PasswordHash,
    string Role,
    bool IsActive);

public sealed record RegisteredUser(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Role);

public sealed class DuplicateUserContactException : Exception
{
}

public interface IUserRegistrationRepository
{
    Task<RegisteredUser> CreateCustomerAsync(
        NewUserRegistration registration,
        CancellationToken cancellationToken = default);
}
