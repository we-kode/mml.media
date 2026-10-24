namespace Media.Application.Models.Federation;

/// <summary>
/// Public information an instance publishes for partners.
/// </summary>
/// <param name="Instance">The instance name.</param>
/// <param name="Version">The federation protocol version.</param>
/// <param name="Endpoint">The federation endpoint.</param>
/// <param name="JwksUri">The url of the public keys.</param>
public record FederationDiscovery(string Instance, string Version, string Endpoint, string JwksUri);
