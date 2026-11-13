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

  /// <summary>
  /// The transfers of the record to partners which are waiting or running.
  /// </summary>
  public IList<RecordSyncTransfer> Transfers { get; set; } = [];

  /// <summary>
  /// The partners the synchronization of the record was revoked for.
  /// </summary>
  public IList<RecordExclusion> Exclusions { get; set; } = [];
}
