namespace LineCom.Api.Infrastructure.Hosting;

public static class ProductionConfigurationGuard
{
    public static void Validate(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default must be configured in production.");
        }

        var storageRootPath = configuration["Storage:RootPath"];
        if (string.IsNullOrWhiteSpace(storageRootPath) || !Path.IsPathRooted(storageRootPath))
        {
            throw new InvalidOperationException(
                "Storage:RootPath must be configured as an absolute path in production.");
        }
    }
}
