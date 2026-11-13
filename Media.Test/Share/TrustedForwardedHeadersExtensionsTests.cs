using Media.API.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class TrustedForwardedHeadersExtensionsTests
{
  [Theory]
  [InlineData("172.18.0.5")]
  [InlineData("10.1.2.3")]
  [InlineData("192.168.10.5")]
  [InlineData("::ffff:172.18.0.5")]
  public async Task DefaultNetworks_AcceptForwardedHeadersFromPrivateNetworks(string proxy)
  {
    var context = await Send(proxy, []);

    Assert.True(context.Request.IsHttps);
    Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
  }

  [Fact]
  public async Task DefaultNetworks_IgnoreForwardedHeadersFromPublicNetworks()
  {
    var context = await Send("198.51.100.1", []);

    Assert.False(context.Request.IsHttps);
    Assert.Equal(IPAddress.Parse("198.51.100.1"), context.Connection.RemoteIpAddress);
  }

  [Fact]
  public async Task ConfiguredNetworks_ReplaceDefaultNetworks()
  {
    var configured = new Dictionary<string, string?> { [$"{TrustedForwardedHeadersExtensions.KNOWN_NETWORKS}:0"] = "198.51.100.0/24" };

    Assert.True((await Send("198.51.100.1", configured)).Request.IsHttps);
    Assert.False((await Send("172.18.0.5", configured)).Request.IsHttps);
  }

  [Fact]
  public void InvalidNetwork_Throws()
  {
    var configured = new Dictionary<string, string?> { [$"{TrustedForwardedHeadersExtensions.KNOWN_NETWORKS}:0"] = "no-network" };

    Assert.Throws<FormatException>(() => Pipeline(configured));
  }

  private static async Task<HttpContext> Send(string proxy, Dictionary<string, string?> configured)
  {
    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse(proxy);
    context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";
    context.Request.Headers["X-Forwarded-Proto"] = "https";
    await Pipeline(configured)(context);
    return context;
  }

  private static RequestDelegate Pipeline(Dictionary<string, string?> configured)
  {
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(configured).Build();
    var services = new ServiceCollection().AddLogging().AddTrustedForwardedHeaders(configuration).BuildServiceProvider();
    return new ApplicationBuilder(services).UseForwardedHeaders().Build();
  }
}
