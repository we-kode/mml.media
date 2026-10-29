using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Media.DBContext.Models;

/// <summary>
/// Marks an own record whose synchronization with a partner was revoked, so it is not sent automatically again.
/// </summary>
[Table("federation_exclusions")]
public class FederationExclusion
{
  /// <summary>
  /// The id of the partner.
  /// </summary>
  public Guid PeerId { get; set; }
  public FederationPeer Peer { get; set; } = null!;

  /// <summary>
  /// The id of the local record.
  /// </summary>
  public Guid RecordId { get; set; }
  public Record Record { get; set; } = null!;

  /// <summary>
  /// Date when the synchronization was revoked.
  /// </summary>
  public DateTime CreatedAt { get; set; }
}
