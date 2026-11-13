namespace Media.Application.Types;

/// <summary>
/// Types of operations queued against a partner instance.
/// </summary>
public enum FederationJobType
{
  /// <summary>
  /// Sends the approval of a registration request.
  /// </summary>
  RegistrationResponse = 1,
  /// <summary>
  /// Informs about new public keys.
  /// </summary>
  KeysChanged = 2,
  /// <summary>
  /// Sends a new record.
  /// </summary>
  PushRecord = 3,
  /// <summary>
  /// Sends changed metadata of a record.
  /// </summary>
  UpdateRecord = 4,
  /// <summary>
  /// Deletes a record on the partner.
  /// </summary>
  DeleteRecord = 5,
  /// <summary>
  /// Removes a record on the partner, but keeps it on this instance.
  /// </summary>
  RevokeRecord = 6,
  /// <summary>
  /// Informs about a new address of this instance.
  /// </summary>
  AddressChanged = 7,
}
