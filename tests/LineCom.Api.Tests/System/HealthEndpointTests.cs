using System.Net;
using System.Net.Http.Json;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.System;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOkResponse()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/system/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("LineCom.Api", body.Service);
    }

    [Fact]
    public async Task GetHealth_ReturnsJsonCamelCaseFields()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/system/health");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"service\":\"LineCom.Api\"", body);
        Assert.DoesNotContain("\"Status\"", body);
        Assert.DoesNotContain("\"Service\"", body);
    }

    [Fact]
    public async Task GetHealth_ReturnsNotFound_ForUnknownPublicSystemRoute()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/system/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostHealth_ReturnsMethodNotAllowed()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/public/system/health", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
    }

    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
