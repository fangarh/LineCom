using LineCom.Api.Infrastructure.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace LineCom.Api.Tests.Infrastructure.Hosting;

public sealed class ReverseProxyForwardingPolicyTests
{
    [Fact]
    public void Configure_TrustsForwardedSchemeHostAndClientAddress_FromReverseProxy()
    {
        var options = new ForwardedHeadersOptions();

        ReverseProxyForwardingPolicy.Configure(options);

        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
        Assert.Equal(1, options.ForwardLimit);
    }
}
