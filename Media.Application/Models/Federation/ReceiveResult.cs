using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// Answer of a partner to a sent record.
/// </summary>
/// <param name="Status">Whether the record was stored.</param>
/// <param name="RecordId">The id of the record on the partner if it was stored.</param>
/// <param name="Offset">The number of bytes the partner received if the transfer is pending.</param>
public record ReceiveResult(ReceiveStatus Status, Guid? RecordId, long Offset = 0)
{
  /// <summary>
  /// A pending transfer continuing at the given offset.
  /// </summary>
  /// <param name="offset">The number of received bytes.</param>
  /// <returns><see cref="ReceiveResult"/></returns>
  public static ReceiveResult Pending(long offset) => new(ReceiveStatus.Pending, null, offset);
}
