using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LineCom.Api.Infrastructure.Hosting;

public static class DevelopmentLoggingPolicy
{
    public static bool ShouldUseDevelopmentConsoleLogging(IWebHostEnvironment environment)
    {
        return environment.IsDevelopment();
    }
}
