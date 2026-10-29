namespace Media.Application.Types;

/// <summary>
/// Directions records can be synchronized.
/// </summary>
public enum SyncDirection
{
  /// <summary>
  /// Records of this instance sent to a partner.
  /// </summary>
  Outgoing = 1,

  /// <summary>
  /// Records of a partner received by this instance.
  /// </summary>
  Incoming = 2,
}
