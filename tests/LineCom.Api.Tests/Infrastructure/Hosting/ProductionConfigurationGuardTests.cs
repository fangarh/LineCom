using LineCom.Api.Infrastructure.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LineCom.Api.Tests.Infrastructure.Hosting;

public sealed class ProductionConfigurationGuardTests
{
    [Fact]
    public void Validate_AllowsDevelopmentFallbackConfiguration()
    {
        var configuration = BuildConfiguration([]);
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Development };

        ProductionConfigurationGuard.Validate(configuration, environment);
    }

    [Fact]
    public void Validate_RejectsBlankConnectionStringInProduction()
    {
        var configuration = BuildConfiguration([
            new("ConnectionStrings:Default", ""),
            new("Storage:RootPath", Path.GetTempPath()),
        ]);
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationGuard.Validate(configuration, environment));

        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void Validate_RejectsBlankStorageRootInProduction()
    {
        var configuration = BuildConfiguration([
            new("ConnectionStrings:Default", "Host=db;Database=linecom;Username=linecom;Password=secret"),
            new("Storage:RootPath", ""),
        ]);
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationGuard.Validate(configuration, environment));

        Assert.Contains("Storage:RootPath", exception.Message);
    }

    [Fact]
    public void Validate_RejectsRelativeStorageRootInProduction()
    {
        var configuration = BuildConfiguration([
            new("ConnectionStrings:Default", "Host=db;Database=linecom;Username=linecom;Password=secret"),
            new("Storage:RootPath", "storage"),
        ]);
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationGuard.Validate(configuration, environment));

        Assert.Contains("absolute path", exception.Message);
    }

    [Fact]
    public void Validate_AllowsProductionWithConnectionStringAndAbsoluteStorageRoot()
    {
        var configuration = BuildConfiguration([
            new("ConnectionStrings:Default", "Host=db;Database=linecom;Username=linecom;Password=secret"),
            new("Storage:RootPath", Path.GetTempPath()),
        ]);
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        ProductionConfigurationGuard.Validate(configuration, environment);
    }

    private static IConfiguration BuildConfiguration(IEnumerable<KeyValuePair<string, string?>> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "LineCom.Api.Tests";

        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
