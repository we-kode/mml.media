using Media.Application.Models.Federation;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Sends requests to partner instances.
/// </summary>
public interface IFederationClient
{
  /// <summary>
  /// Loads the public information of the instance with the given base url.
  /// </summary>
  /// <param name="baseUrl">The base url of the instance.</param>
  /// <returns><see cref="FederationDiscovery"/></returns>
  /// <exception cref="FederationException">If the url is not allowed, the instance is not reachable or the response is invalid.</exception>
  Task<FederationDiscovery> GetDiscovery(string baseUrl);

  /// <summary>
  /// Loads the public keys published at the given url.
  /// </summary>
  /// <param name="jwksUri">The url of the public keys.</param>
  /// <returns>The JSON web key set.</returns>
  /// <exception cref="FederationException">If the url is not allowed, the instance is not reachable or the response is invalid.</exception>
  Task<string> GetJwks(string jwksUri);

  /// <summary>
  /// Sends a request authenticated with a signed assertion to a partner.
  /// </summary>
  /// <param name="endpoint">The federation endpoint of the partner.</param>
  /// <param name="targetInstance">The instance name of the partner.</param>
  /// <param name="method">The http method.</param>
  /// <param name="path">The path relative to the endpoint.</param>
  /// <param name="content">The request content.</param>
  /// <param name="claims">Additional claims of the assertion.</param>
  /// <returns>The response of the partner.</returns>
  /// <exception cref="FederationException">If the url is not allowed or the partner is not reachable.</exception>
  Task<HttpResponseMessage> Send(string endpoint, string targetInstance, HttpMethod method, string path, HttpContent? content = null, IDictionary<string, object>? claims = null);
}
