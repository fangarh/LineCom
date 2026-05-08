using Microsoft.Extensions.FileProviders;

namespace LineCom.Api.Infrastructure.Hosting;

public static class LocalStorageStaticFilesExtensions
{
    public static IApplicationBuilder UseLocalStorageStaticFiles(
        this WebApplication app,
        IConfiguration configuration)
    {
        var rootPath = configuration["Storage:RootPath"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            rootPath = Path.Combine(app.Environment.ContentRootPath, "storage");
        }

        if (!Path.IsPathRooted(rootPath))
        {
            rootPath = Path.Combine(app.Environment.ContentRootPath, rootPath);
        }

        Directory.CreateDirectory(rootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(rootPath),
            RequestPath = "/storage",
        });

        return app;
    }
}
