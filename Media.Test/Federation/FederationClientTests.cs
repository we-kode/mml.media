using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationClientTests
{
  private sealed class RecordingHandler : HttpMessageHandler
  {
    public Uri? RequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      RequestUri = request.RequestUri;
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
  }

  private static (FederationClient Client, RecordingHandler Handler) Create()
  {
    var handler = new RecordingHandler();
    var assertions = new Mock<IFederationAssertionService>();
    assertions.Setup(a => a.Create(It.IsAny<string>(), It.IsAny<IDictionary<string, object>?>())).Returns("token");
    return (new FederationClient(new HttpClient(handler), assertions.Object), handler);
  }

  [Fact]
  public async Task Send_KeepsQueryOfPath()
  {
    var (client, handler) = Create();

    using var response = await client.Send("https://beta.example/api/v2.0/media/federation/", "beta", HttpMethod.Post, "records/1/content?offset=5");

    Assert.Equal("https://beta.example/api/v2.0/media/federation/records/1/content?offset=5", handler.RequestUri!.AbsoluteUri);
  }

  [Theory]
  [InlineData("http://beta.example/api")]
  [InlineData("https://beta.example/api?x=1")]
  public async Task Send_RejectsInvalidEndpoint(string endpoint)
  {
    var (client, _) = Create();

    var exception = await Assert.ThrowsAsync<FederationException>(() => client.Send(endpoint, "beta", HttpMethod.Post, "records"));
    Assert.Equal(FederationErrors.InvalidUrl, exception.Code);
  }
}
