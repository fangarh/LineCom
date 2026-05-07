using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Auth.Services;

internal static class AuthErrors
{
    public static ApiException InvalidRequest()
    {
        return new ApiException(
            "validation.invalid_request",
            "Некорректные данные запроса.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException InvalidContact()
    {
        return new ApiException(
            "auth.invalid_contact",
            "Укажите email или телефон.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException InvalidPassword()
    {
        return new ApiException(
            "auth.invalid_password",
            "Некорректный пароль.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException UserAlreadyExists()
    {
        return new ApiException(
            "auth.user_already_exists",
            "Пользователь с таким email или телефоном уже существует.",
            StatusCodes.Status409Conflict);
    }

    public static ApiException InvalidCredentials()
    {
        return new ApiException(
            "auth.invalid_credentials",
            "Неверный логин или пароль.",
            StatusCodes.Status401Unauthorized);
    }

    public static ApiException Unauthorized()
    {
        return new ApiException(
            "auth.unauthorized",
            "Требуется вход в аккаунт.",
            StatusCodes.Status401Unauthorized);
    }

    public static ApiException UserInactive()
    {
        return new ApiException(
            "auth.user_inactive",
            "Аккаунт отключен.",
            StatusCodes.Status403Forbidden);
    }

    public static ApiException Forbidden()
    {
        return new ApiException(
            "auth.forbidden",
            "Недостаточно прав.",
            StatusCodes.Status403Forbidden);
    }
}
