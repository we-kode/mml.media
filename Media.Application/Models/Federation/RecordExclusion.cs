using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// A partner an own record is excluded from the synchronization with.
/// </summary>
public class RecordExclusion
{
  /// <summary>
  /// Id of the partner.
  /// </summary>
  public Guid PeerId { get; set; }

  /// <summary>
  /// The instance name of the partner.
  /// </summary>
  public string Instance { get; set; } = string.Empty;

  /// <summary>
  /// Date when the synchronization was revoked.
  /// </summary>
  public DateTime CreatedAt { get; set; }
}
