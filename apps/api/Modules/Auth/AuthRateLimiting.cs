using System.Threading.RateLimiting;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.RateLimiting;

namespace LineCom.Api.Modules.Auth;

public static class AuthRateLimiting
{
    public const string PolicyName = "auth-attempts";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var endpointPath = httpContext.Request.Path.Value ?? string.Empty;
                var partitionKey = $"{remoteIp}|{endpointPath}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ApiErrorResponse("auth.rate_limited", "Слишком много попыток. Попробуйте позже."),
                    cancellationToken);
            };
        });

        return services;
    }
}
