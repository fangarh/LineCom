using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

namespace LineCom.Api.Modules.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IWebHostEnvironment? environment = null)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "linecom_auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment?.IsProduction() == true
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.LoginPath = "/api/auth/login";
                options.AccessDeniedPath = "/api/auth/forbidden";
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context => WriteAuthErrorAsync(
                        context.Response,
                        StatusCodes.Status401Unauthorized,
                        "auth.unauthorized",
                        "Требуется вход в аккаунт."),
                    OnRedirectToAccessDenied = context => WriteAuthErrorAsync(
                        context.Response,
                        StatusCodes.Status403Forbidden,
                        "auth.forbidden",
                        "Недостаточно прав.")
                };
            });

        services.AddScoped<ICustomerRegistrationService, CustomerRegistrationService>();
        services.AddScoped<IUserRegistrationRepository, DapperUserRegistrationRepository>();
        services.AddScoped<ICustomerLoginService, CustomerLoginService>();
        services.AddScoped<IUserLoginRepository, DapperUserLoginRepository>();
        services.AddScoped<IAuthSessionService, CookieAuthSessionService>();
        services.AddScoped<IAuthCurrentUserService, AuthCurrentUserService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }

    private static async Task WriteAuthErrorAsync(
        HttpResponse response,
        int statusCode,
        string code,
        string message)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            response.Body,
            new ApiErrorResponse(code, message),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            response.HttpContext.RequestAborted);
    }
}
