using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LineCom.Api.Modules.Auth.Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCsrfTokenAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-CSRF-Token";

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var expectedToken = httpContext.User.FindFirstValue(CookieAuthSessionService.CsrfClaimType);
        if (string.IsNullOrWhiteSpace(expectedToken) ||
            !httpContext.Request.Headers.TryGetValue(HeaderName, out var submittedTokens) ||
            submittedTokens.Count != 1 ||
            !TokenEquals(expectedToken, submittedTokens[0]))
        {
            throw AuthErrors.Forbidden();
        }

        return Task.CompletedTask;
    }

    private static bool TokenEquals(string expectedToken, string? submittedToken)
    {
        if (string.IsNullOrWhiteSpace(submittedToken))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        var submittedBytes = Encoding.UTF8.GetBytes(submittedToken);
        return expectedBytes.Length == submittedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, submittedBytes);
    }
}
