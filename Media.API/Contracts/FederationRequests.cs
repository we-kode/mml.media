using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Request to register at a partner instance.
/// </summary>
public class PeerRegistrationRequest
{
  /// <summary>
  /// The public https base url of the partner.
  /// </summary>
  [Required]
  [MaxLength(2048)]
  public string BaseUrl { get; set; } = string.Empty;

  /// <summary>
  /// The invitation code created by the admin of the partner.
  /// </summary>
  [Required]
  [MaxLength(64)]
  public string InvitationCode { get; set; } = string.Empty;
}

/// <summary>
/// Request to remove the registration of a partner instance.
/// </summary>
public class PeerUnlinkRequest
{
  /// <summary>
  /// Removes the records of the partner on this instance and asks the partner to remove the records of this instance.
  /// </summary>
  public bool DeleteRecords { get; set; }

  /// <summary>
  /// Removes the registration even if the partner cannot be informed.
  /// </summary>
  public bool Force { get; set; }
}

/// <summary>
/// Request to create or change a federation rule. Values of one criterion are combined with OR, criteria with AND.
/// </summary>
public class RuleRequest
{
  /// <summary>
  /// The direction the rule is applied to.
  /// </summary>
  [Required]
  [EnumDataType(typeof(SyncDirection))]
  public SyncDirection Direction { get; set; }

  /// <summary>
  /// Ids of the partners the rule is restricted to. Empty applies to all partners.
  /// </summary>
  public IList<Guid> Peers { get; set; } = [];

  /// <summary>
  /// Ids of the artists the rule matches. Empty matches all artists.
  /// </summary>
  public IList<Guid> Artists { get; set; } = [];

  /// <summary>
  /// Ids of the albums the rule matches. Empty matches all albums.
  /// </summary>
  public IList<Guid> Albums { get; set; } = [];

  /// <summary>
  /// Ids of the genres the rule matches. Empty matches all genres.
  /// </summary>
  public IList<Guid> Genres { get; set; } = [];

  /// <summary>
  /// Incoming rules: ids of the groups received records are assigned to.
  /// Outgoing rules: only records in one of these groups match, empty matches all records.
  /// </summary>
  public IList<Guid> Groups { get; set; } = [];
}

/// <summary>
/// Request to create an invitation for a partner.
/// </summary>
public class InvitationRequest
{
  /// <summary>
  /// Hours the invitation stays valid, at most 90 days.
  /// </summary>
  [Range(1, Invitation.MaxValidityHours)]
  public int ValidityHours { get; set; } = Invitation.DefaultValidityHours;
}

/// <summary>
/// Request to synchronize records manually.
/// </summary>
public class SyncRequest
{
  /// <summary>
  /// Ids of the records.
  /// </summary>
  public IList<Guid> RecordIds { get; set; } = [];

  /// <summary>
  /// Folders whose records are synchronized.
  /// </summary>
  public IList<RecordFolder> Folders { get; set; } = [];
}

/// <summary>
/// Request of a partner instance to remove the registration.
/// </summary>
public class RemoteUnlinkRequest
{
  /// <summary>
  /// Whether the records owned by the requesting partner are removed.
  /// </summary>
  public bool DeleteOwnerRecords { get; set; }
}
