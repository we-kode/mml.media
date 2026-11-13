using Media.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Sockets;

/// <summary>
/// SignalR hub informing admins about registrations and synchronizations of partner instances.
/// </summary>
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
public class FederationHub : Hub
{
  /// <summary>
  /// Event sent when a partner requests a registration.
  /// </summary>
  public const string RequestReceived = "FEDERATION_REQUEST_RECEIVED";

  /// <summary>
  /// Event sent when a partner is changed or removed.
  /// </summary>
  public const string StateChanged = "FEDERATION_STATE_CHANGED";

  /// <summary>
  /// Event sent when a new audit log entry is written.
  /// </summary>
  public const string AuditAdded = "FEDERATION_AUDIT_ADDED";

  /// <summary>
  /// Event sent when a part of a record was transferred.
  /// </summary>
  public const string Progress = "FEDERATION_PROGRESS";
}
