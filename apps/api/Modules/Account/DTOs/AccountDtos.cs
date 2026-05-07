using LineCom.Api.Modules.Auth.DTOs;

namespace LineCom.Api.Modules.Account.DTOs;

public sealed record AccountProfileDto(
    CurrentUserDto User,
    AccountOrganizationDto? Organization);

public sealed record AccountOrganizationDto(
    string Name,
    string? Inn,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Comment);

public sealed record UpdateAccountProfileRequest(
    string? Name,
    string? Email,
    string? Phone);

public sealed record UpsertAccountOrganizationRequest(
    string? Name,
    string? Inn,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Comment);
