using Media.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.SystemNetHttp;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace Media.Test;

public class StartupTests
{
  [Fact]
  public void ConfigureAuth_SendsApiClientCredentialsWithOpenIddictValidationRequests()
  {
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["OpenId:Issuer"] = "https://identity.example/",
      ["ApiClient:ClientId"] = "media",
      ["ApiClient:ClientSecret"] = "secret",
    }).Build();
    var services = new ServiceCollection();
    services.AddLogging();
    new Startup(configuration).ConfigureAuth(services);
    using var provider = services.BuildServiceProvider();

    using var client = new HttpClient();
    foreach (var action in provider.GetRequiredService<IOptionsMonitor<OpenIddictValidationSystemNetHttpOptions>>().CurrentValue.HttpClientActions)
    {
      action(client);
    }

    Assert.Equal("media", client.DefaultRequestHeaders.GetValues("ClientId").Single());
    Assert.Equal("secret", client.DefaultRequestHeaders.GetValues("ClientSecret").Single());
  }
}
