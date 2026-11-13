using Media.Application.Types;
using System;
using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// One entry of the federation audit log.
/// </summary>
public class AuditEntry
{
  /// <summary>
  /// Id of the entry.
  /// </summary>
  public Guid EntryId { get; set; }

  /// <summary>
  /// Date when the entry was created.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  /// The action which was performed.
  /// </summary>
  public AuditAction Action { get; set; }

  /// <summary>
  /// The result of the action.
  /// </summary>
  public AuditStatus Status { get; set; }

  /// <summary>
  /// The instance the action originated from.
  /// </summary>
  public string Source { get; set; } = string.Empty;

  /// <summary>
  /// The instance the action targeted.
  /// </summary>
  public string Target { get; set; } = string.Empty;

  /// <summary>
  /// The direction of the action seen from this instance.
  /// </summary>
  public SyncDirection Direction { get; set; }

  /// <summary>
  /// The partner instance concerned by the action.
  /// </summary>
  public string Instance { get; set; } = string.Empty;

  /// <summary>
  /// The id of the related transaction if available.
  /// </summary>
  public Guid? TransactionId { get; set; }

  /// <summary>
  /// The ids of the affected records.
  /// </summary>
  public IList<Guid> RecordIds { get; set; } = [];

  /// <summary>
  /// The affected records, which still exist, with their current titles.
  /// </summary>
  public IList<AuditRecord> Records { get; set; } = [];

  /// <summary>
  /// Additional information or the error message.
  /// </summary>
  public string? Message { get; set; }

  /// <summary>
  /// The number of transferred bytes if the entry is a running transfer.
  /// </summary>
  public long? Transferred { get; set; }

  /// <summary>
  /// The size of the file in bytes if the entry is a running transfer.
  /// </summary>
  public long? Size { get; set; }
}
