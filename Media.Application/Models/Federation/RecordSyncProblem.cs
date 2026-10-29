using Media.Application.Types;
using System;

namespace Media.Application.Models.Federation;

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
