using Media.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test;

public class ApiVersioningTests : IAsyncLifetime
{
  private const string RecordId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

  private WebApplication? app;
  private HttpClient? client;

  public async ValueTask InitializeAsync()
  {
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.AddControllers().AddApplicationPart(typeof(Startup).Assembly);
    Startup.ConfigureApiServices(builder.Services);
    app = builder.Build();
    app.MapControllers();
    await app.StartAsync(TestContext.Current.CancellationToken);
    client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First()) };
  }

  public async ValueTask DisposeAsync()
  {
    client?.Dispose();
    if (app != null)
    {
      await app.DisposeAsync();
    }
  }

  [Theory]
  // admin app and app up to 2.x
  [InlineData("POST", "api/v1.0/media/record/list")]
  [InlineData("GET", "api/v1.0/media/record/artists")]
  [InlineData("POST", "api/v1.0/media/record/bitrates")]
  [InlineData("POST", "api/v1.0/media/livestream/list")]
  [InlineData("POST", "api/v1.0/media/notification/list")]
  [InlineData("GET", "api/v1.0/media/settings")]
  [InlineData("GET", "api/v1.0/media/federation/peers")]
  [InlineData("GET", $"api/v1.0/media/stream/{RecordId}")]
  // app 3.x and federation partners
  [InlineData("GET", "api/v2.0/media/album/albums")]
  [InlineData("GET", "api/v2.0/media/artist/artists")]
  [InlineData("GET", "api/v2.0/media/genre/genres")]
  [InlineData("GET", "api/v2.0/media/language/languages")]
  [InlineData("POST", "api/v2.0/media/record/list")]
  [InlineData("POST", "api/v2.0/media/record/check")]
  [InlineData("GET", $"api/v2.0/media/stream/{RecordId}")]
  [InlineData("GET", "api/v2.0/media/federation/peer/discovery")]
  // share pages without api version
  [InlineData("GET", "media/s/open")]
  public async Task Request_WithVersionUsedByClients_IsRouted(string method, string path)
  {
    var response = await client!.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);
    Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Theory]
  [InlineData("GET", "api/v1.0/media/album/albums")]
  [InlineData("GET", "api/v2.0/media/record/artists")]
  [InlineData("GET", "api/v3.0/media/settings")]
  public async Task Request_WithUnsupportedVersion_IsRejected(string method, string path)
  {
    var response = await client!.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }
}
