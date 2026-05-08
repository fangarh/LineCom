using LineCom.Api.Infrastructure.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LineCom.Api.Tests.Infrastructure.Hosting;

public sealed class HttpsRedirectionPolicyTests
{
    [Fact]
    public void ShouldUseHttpsRedirection_ReturnsFalse_InDevelopment()
    {
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Development };

        Assert.False(HttpsRedirectionPolicy.ShouldUseHttpsRedirection(environment));
    }

    [Fact]
    public void ShouldUseHttpsRedirection_ReturnsTrue_OutsideDevelopment()
    {
        var environment = new TestWebHostEnvironment { EnvironmentName = Environments.Production };

        Assert.True(HttpsRedirectionPolicy.ShouldUseHttpsRedirection(environment));
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
