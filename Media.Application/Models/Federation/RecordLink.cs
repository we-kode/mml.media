using Media.Application.Types;
using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// Maps a local record to the record of a partner it is synchronized with.
/// </summary>
public class RecordLink
{
  /// <summary>
  /// Id of the link.
  /// </summary>
  public Guid LinkId { get; set; }

  /// <summary>
  /// Id of the partner.
  /// </summary>
  public Guid PeerId { get; set; }

  /// <summary>
  /// The instance name of the partner. Only set when the links of a record are listed.
  /// </summary>
  public string? Instance { get; set; }

  /// <summary>
  /// Id of the local record.
  /// </summary>
  public Guid LocalRecordId { get; set; }

  /// <summary>
  /// Id of the record on the partner.
  /// </summary>
  public Guid RemoteRecordId { get; set; }

  /// <summary>
  /// The direction the record was synchronized.
  /// </summary>
  public SyncDirection Direction { get; set; }

  /// <summary>
  /// The id of the transaction shared by both instances.
  /// </summary>
  public Guid TransactionId { get; set; }

  /// <summary>
  /// Date when the record was synchronized.
  /// </summary>
  public DateTime SyncedAt { get; set; }

  /// <summary>
  /// Date when the metadata were synchronized the last time.
  /// </summary>
  public DateTime UpdatedAt { get; set; }
}
