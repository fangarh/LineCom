using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LineCom.Api.Tests.Support;

internal sealed class LineComWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(FindApiContentRoot());
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var projectFile = Path.Combine(directory.FullName, "apps", "api", "LineCom.Api.csproj");
            if (File.Exists(projectFile))
            {
                return Path.GetDirectoryName(projectFile)
                    ?? throw new InvalidOperationException($"API content root path is invalid: {projectFile}");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find apps/api/LineCom.Api.csproj from the test output path.");
    }
}
