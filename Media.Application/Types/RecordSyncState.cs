namespace Media.Application.Types;

/// <summary>
/// The synchronization state of a record with partner instances.
/// </summary>
public enum RecordSyncState
{
  /// <summary>
  /// The record is an own record, which is not sent to any partner.
  /// </summary>
  None = 0,

  /// <summary>
  /// The record was received from a partner.
  /// </summary>
  Received = 1,

  /// <summary>
  /// The record was sent to at least one partner.
  /// </summary>
  Sent = 2,

  /// <summary>
  /// An operation to synchronize the record with a partner failed.
  /// </summary>
  Problem = 3,
}
