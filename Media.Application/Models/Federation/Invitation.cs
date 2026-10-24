using System;
using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A one-time code a partner must present to request a registration.
/// </summary>
public class Invitation
{
  /// <summary>
  /// Hours a new invitation stays valid if no validity is given.
  /// </summary>
  public const int DefaultValidityHours = 24;

  /// <summary>
  /// Maximum hours an invitation can stay valid.
  /// </summary>
  public const int MaxValidityHours = 90 * 24;

  /// <summary>
  /// Id of the invitation.
  /// </summary>
  public Guid InvitationId { get; set; }

  /// <summary>
  /// The code to pass to the admin of the partner. Only available when the invitation is created.
  /// </summary>
  public string? Code { get; set; }

  /// <summary>
  /// The public address of this instance, which the partner needs together with the code. Only available when the invitation is created.
  /// </summary>
  public string? BaseUrl { get; set; }

  /// <summary>
  /// Date when the invitation was created.
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Date when the invitation expires.
  /// </summary>
  public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// A list of open invitations.
/// </summary>
public class Invitations
{
  /// <summary>
  /// The total count of open invitations.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded invitations.
  /// </summary>
  public IList<Invitation> Items { get; set; } = [];
}
