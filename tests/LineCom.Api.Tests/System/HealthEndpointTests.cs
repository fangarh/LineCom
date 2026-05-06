using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.System;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOkResponse()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/system/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("LineCom.Api", body.Service);
    }

    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
