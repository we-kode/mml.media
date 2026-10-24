using Media.Application.Types;
using System;
using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// The synchronization details of one record.
/// </summary>
public class RecordSyncInfo
{
  /// <summary>
  /// The partners the record was sent to or received from.
  /// </summary>
  public IList<RecordLink> Links { get; set; } = [];

  /// <summary>
  /// The operations of the record which failed or are retried after an error.
  /// </summary>
  public IList<RecordSyncProblem> Problems { get; set; } = [];
}

/// <summary>
/// A failed operation to synchronize a record with a partner.
/// </summary>
public class RecordSyncProblem
{
  /// <summary>
  /// The instance name of the partner.
  /// </summary>
  public string Instance { get; set; } = string.Empty;

  /// <summary>
  /// The failed operation.
  /// </summary>
  public FederationJobType Type { get; set; }

  /// <summary>
  /// The last error message.
  /// </summary>
  public string? Error { get; set; }

  /// <summary>
  /// Indicates whether the operation failed finally. Otherwise it is retried.
  /// </summary>
  public bool Failed { get; set; }

  /// <summary>
  /// Date of the next attempt, if the operation is retried.
  /// </summary>
  public DateTime? NextAttemptAt { get; set; }
}
