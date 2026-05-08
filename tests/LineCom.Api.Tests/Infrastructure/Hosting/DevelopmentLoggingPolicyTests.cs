using LineCom.Api.Infrastructure.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LineCom.Api.Tests.Infrastructure.Hosting;

public sealed class DevelopmentLoggingPolicyTests
{
    [Fact]
    public void ShouldUseDevelopmentConsoleLogging_ReturnsTrue_InDevelopment()
    {
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Development };

        Assert.True(DevelopmentLoggingPolicy.ShouldUseDevelopmentConsoleLogging(environment));
    }

    [Fact]
    public void ShouldUseDevelopmentConsoleLogging_ReturnsFalse_OutsideDevelopment()
    {
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        Assert.False(DevelopmentLoggingPolicy.ShouldUseDevelopmentConsoleLogging(environment));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "LineCom.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
