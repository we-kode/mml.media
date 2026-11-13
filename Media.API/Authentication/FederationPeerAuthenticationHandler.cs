using Media.Application.Contracts.Services;
using Media.Application.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Media.API.Authentication;

/// <summary>
/// Authenticates partner instances by the signed assertion sent with each request.
/// </summary>
public class FederationPeerAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder,
  IFederationService federationService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  /// <summary>
  /// Name of the authentication scheme.
  /// </summary>
  public const string SchemeName = "FederationPeer";

  /// <summary>
  /// Claim containing the id of the authenticated partner.
  /// </summary>
  public const string PeerIdClaim = "peer_id";

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var header = Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
      return AuthenticateResult.NoResult();
    }

    var authenticated = await federationService.Authenticate(header["Bearer ".Length..].Trim(), PeerState.PendingApproval, PeerState.ResponsePending, PeerState.Linked);
    if (authenticated == null)
    {
      return AuthenticateResult.Fail("Invalid assertion.");
    }

    var peer = authenticated.Value.Peer;
    var identity = new ClaimsIdentity(
      [
        new Claim(ClaimTypes.NameIdentifier, peer.Instance),
        new Claim(PeerIdClaim, peer.PeerId.ToString()),
        new Claim(ClaimTypes.Role, peer.State.ToString()),
      ], SchemeName);
    return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
  }
}
