using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Account.Repositories;

public sealed record AccountOrganizationRecord(
    string Name,
    string? Inn,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Comment);

public sealed record AccountProfileUpdate(
    string Name,
    string? Email,
    string? Phone);

public sealed record AccountOrganizationUpsert(
    string Name,
    string? Inn,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Comment);

public interface IAccountProfileRepository
{
    Task<AccountOrganizationRecord?> FindOrganizationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CurrentUserDto> UpdateProfileAsync(
        Guid userId,
        AccountProfileUpdate profile,
        CancellationToken cancellationToken = default);

    Task<AccountOrganizationRecord> UpsertOrganizationAsync(
        Guid userId,
        AccountOrganizationUpsert organization,
        CancellationToken cancellationToken = default);
}
