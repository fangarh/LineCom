using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LineCom.Api.Infrastructure.Hosting;

public static class HttpsRedirectionPolicy
{
    public static bool ShouldUseHttpsRedirection(IWebHostEnvironment environment)
    {
        return !environment.IsDevelopment();
    }
}
