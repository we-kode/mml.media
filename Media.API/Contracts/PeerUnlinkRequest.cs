namespace Media.API.Contracts;

/// <summary>
/// Request to remove the registration of a partner instance.
/// </summary>
public class PeerUnlinkRequest
{
  /// <summary>
  /// Removes the records of the partner on this instance and asks the partner to remove the records of this instance.
  /// </summary>
  public bool DeleteRecords { get; set; }

  /// <summary>
  /// Removes the registration even if the partner cannot be informed.
  /// </summary>
  public bool Force { get; set; }
}
