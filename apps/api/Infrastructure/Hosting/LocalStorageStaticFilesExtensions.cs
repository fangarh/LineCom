using Microsoft.Extensions.FileProviders;

namespace LineCom.Api.Infrastructure.Hosting;

public static class LocalStorageStaticFilesExtensions
{
    public static IApplicationBuilder UseLocalStorageStaticFiles(
        this WebApplication app,
        IConfiguration configuration)
    {
        var rootPath = LocalStoragePathPolicy.ResolveRootPath(
            configuration["Storage:RootPath"],
            app.Environment.ContentRootPath);
        foreach (var publicPrefix in LocalStoragePathPolicy.PublicPrefixes)
        {
            var physicalDirectory = Path.Combine(rootPath, publicPrefix.Directory);
            Directory.CreateDirectory(physicalDirectory);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(physicalDirectory),
                RequestPath = publicPrefix.RequestPath,
            });
        }

        return app;
    }
}
