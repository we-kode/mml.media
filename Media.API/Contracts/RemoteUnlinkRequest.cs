namespace Media.API.Contracts;

/// <summary>
/// Request of a partner instance to remove the registration.
/// </summary>
public class RemoteUnlinkRequest
{
  /// <summary>
  /// Whether the records owned by the requesting partner are removed.
  /// </summary>
  public bool DeleteOwnerRecords { get; set; }
}
