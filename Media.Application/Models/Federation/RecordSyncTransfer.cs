using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// A waiting or running transfer of a record to a partner.
/// </summary>
public class RecordSyncTransfer
{
  /// <summary>
  /// The instance name of the partner.
  /// </summary>
  public string Instance { get; set; } = string.Empty;

  /// <summary>
  /// The number of bytes the partner received so far.
  /// </summary>
  public long Transferred { get; set; }

  /// <summary>
  /// The size of the file in bytes or null if the transfer did not start yet.
  /// </summary>
  public long? Size { get; set; }

  /// <summary>
  /// Date of the next attempt.
  /// </summary>
  public DateTime NextAttemptAt { get; set; }
}
