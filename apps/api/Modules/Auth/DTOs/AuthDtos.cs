namespace LineCom.Api.Modules.Auth.DTOs;

public sealed record RegisterRequest(
    string? Name,
    string? Email,
    string? Phone,
    string? Password);

public sealed record LoginRequest(
    string? Login,
    string? Password);

public sealed record AuthSessionDto(
    CurrentUserDto User,
    string CsrfToken);

public sealed record CurrentUserDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Role);
