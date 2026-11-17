using Media.API.Extensions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using Xunit;

namespace Media.Test;

public class ConfigurationExtensionsTests
{
  private static IConfiguration Build(string? host, string? user, string? password, string? virtualHost) =>
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["MessageBus:Host"] = host,
      ["MessageBus:User"] = user,
      ["MessageBus:Password"] = password,
      ["MessageBus:VirtualHost"] = virtualHost,
    }).Build();

  [Theory]
  [InlineData("guest", "guest")]
  [InlineData("mml@user", "p@ss:w/rd#?%;,+ ")]
  public void GetMessageBusConnectionString_RoundTripsCredentials(string user, string password)
  {
    var connection = Build("wekode.mml.mbus", user, password, "/").GetMessageBusConnectionString();
    var factory = new ConnectionFactory { Uri = new Uri(connection) };

    Assert.Equal("wekode.mml.mbus", factory.HostName);
    Assert.Equal(user, factory.UserName);
    Assert.Equal(password, factory.Password);
    Assert.Equal("/", factory.VirtualHost);
  }

  [Theory]
  [InlineData(null, "guest", "guest")]
  [InlineData("wekode.mml.mbus", null, "guest")]
  [InlineData("wekode.mml.mbus", "guest", null)]
  public void GetMessageBusConnectionString_ThrowsWhenRequiredValueIsMissing(string? host, string? user, string? password)
  {
    Assert.Throws<ArgumentNullException>(() => Build(host, user, password, null).GetMessageBusConnectionString());
  }
}
