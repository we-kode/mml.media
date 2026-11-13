using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class FederationClient(HttpClient httpClient, IFederationAssertionService assertions) : IFederationClient
{
  private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(30);

  public async Task<FederationDiscovery> GetDiscovery(string baseUrl)
  {
    var baseUri = FederationHttpHandler.ValidateUrl(baseUrl);
    FederationDiscovery? discovery;
    try
    {
      using var cts = new CancellationTokenSource(MetadataTimeout);
      using var response = await httpClient.GetAsync(FederationOptions.DiscoveryUri(baseUri.ToString()), cts.Token).ConfigureAwait(false);
      if (!response.IsSuccessStatusCode)
      {
        throw new FederationException(FederationErrors.InvalidDiscovery, $"Discovery returned {(int)response.StatusCode}.");
      }

      discovery = await response.Content.ReadFromJsonAsync<FederationDiscovery>(cts.Token).ConfigureAwait(false);
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    {
      throw new FederationException(FederationErrors.RemoteUnreachable, e.Message);
    }
    catch (JsonException)
    {
      throw new FederationException(FederationErrors.InvalidDiscovery);
    }

    if (discovery == null
      || !FederationOptions.IsValidInstanceName(discovery.Instance)
      || discovery.Version != FederationOptions.ProtocolVersion
      || !IsSameOrigin(baseUri, discovery.Endpoint)
      || !IsSameOrigin(baseUri, discovery.JwksUri))
    {
      throw new FederationException(FederationErrors.InvalidDiscovery);
    }

    return discovery;
  }

  public async Task<string> GetJwks(string jwksUri)
  {
    var uri = FederationHttpHandler.ValidateUrl(jwksUri);
    try
    {
      using var cts = new CancellationTokenSource(MetadataTimeout);
      using var response = await httpClient.GetAsync(uri, cts.Token).ConfigureAwait(false);
      if (!response.IsSuccessStatusCode)
      {
        throw new FederationException(FederationErrors.InvalidDiscovery, $"Key set returned {(int)response.StatusCode}.");
      }

      var jwks = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
      if (new JsonWebKeySet(jwks).Keys.Count == 0)
      {
        throw new FederationException(FederationErrors.InvalidDiscovery, "Key set is empty.");
      }

      return jwks;
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    {
      throw new FederationException(FederationErrors.RemoteUnreachable, e.Message);
    }
    catch (ArgumentException)
    {
      throw new FederationException(FederationErrors.InvalidDiscovery, "Key set is invalid.");
    }
  }

  public async Task<HttpResponseMessage> Send(string endpoint, string targetInstance, HttpMethod method, string path, HttpContent? content = null, IDictionary<string, object>? claims = null)
  {
    // only the endpoint of the partner is validated, the path may contain query parameters, e.g. the offset of a chunk.
    var baseUri = FederationHttpHandler.ValidateUrl(endpoint);
    var uri = new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}/{path.TrimStart('/')}");
    using var request = new HttpRequestMessage(method, uri) { Content = content };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", assertions.Create(targetInstance, claims));
    try
    {
      return await httpClient.SendAsync(request).ConfigureAwait(false);
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    {
      throw new FederationException(FederationErrors.RemoteUnreachable, e.Message);
    }
  }

  private static bool IsSameOrigin(Uri baseUri, string? url)
  {
    return Uri.TryCreate(url, UriKind.Absolute, out var uri)
      && uri.Scheme == Uri.UriSchemeHttps
      && string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
      && uri.Port == baseUri.Port;
  }
}
