namespace Media.Application.Models.Federation;

/// <summary>
/// Results of receiving a record.
/// </summary>
public enum ReceiveStatus
{
  /// <summary>
  /// The record was stored.
  /// </summary>
  Created = 1,

  /// <summary>
  /// The record exists already and was not stored again.
  /// </summary>
  Skipped = 2,

  /// <summary>
  /// The transfer is not finished. The sender continues at the returned offset.
  /// </summary>
  Pending = 3,
}
