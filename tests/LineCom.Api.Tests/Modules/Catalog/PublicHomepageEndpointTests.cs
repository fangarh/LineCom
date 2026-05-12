using System.Net;
using System.Text.Json;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Queries;
using LineCom.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class PublicHomepageEndpointTests
{
    [Fact]
    public async Task GetSections_ReturnsPublicHomepageSections()
    {
        var responseBody = new PublicHomepageSectionsResponse(
        [
            new PublicHomepageSectionDto(
                "featured_products",
                "Главные товары",
                "product",
                [
                    new PublicHomepageSectionItemDto(
                        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                        Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
                        null,
                        "Кабель U/UTP",
                        "u-utp-cable",
                        "SKU-1")
                ])
        ]);

        await using var factory = CreateFactory(responseBody);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/homepage/sections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await JsonSerializer.DeserializeAsync<PublicHomepageSectionsResponse>(
            await response.Content.ReadAsStreamAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        var section = Assert.Single(body.Sections);
        Assert.Equal("featured_products", section.Code);
        Assert.Equal("Главные товары", section.Title);

        var item = Assert.Single(section.Items);
        Assert.Equal("u-utp-cable", item.Slug);
        Assert.Equal("SKU-1", item.SecondaryText);
    }

    [Fact]
    public async Task GetSections_DoesNotRequireAuthentication()
    {
        await using var factory = CreateFactory(new PublicHomepageSectionsResponse([]));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/public/homepage/sections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostSections_ReturnsMethodNotAllowed()
    {
        await using var factory = CreateFactory(new PublicHomepageSectionsResponse([]));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/public/homepage/sections", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(PublicHomepageSectionsResponse responseBody)
    {
        return new LineComWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPublicHomepageQuery>();
                    services.AddSingleton<IPublicHomepageQuery>(new StubPublicHomepageQuery(responseBody));
                });
            });
    }

    private sealed class StubPublicHomepageQuery : IPublicHomepageQuery
    {
        private readonly PublicHomepageSectionsResponse _responseBody;

        public StubPublicHomepageQuery(PublicHomepageSectionsResponse responseBody)
        {
            _responseBody = responseBody;
        }

        public Task<PublicHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_responseBody);
        }
    }
}
