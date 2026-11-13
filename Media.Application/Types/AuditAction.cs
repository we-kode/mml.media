namespace Media.Application.Types;

/// <summary>
/// Actions logged in the federation audit log.
/// </summary>
public enum AuditAction
{
  /// <summary>
  /// This instance requested a registration at a partner.
  /// </summary>
  RegistrationRequested = 1,
  /// <summary>
  /// A partner requested a registration at this instance.
  /// </summary>
  RegistrationReceived = 2,
  /// <summary>
  /// A registration request was approved.
  /// </summary>
  RegistrationApproved = 3,
  /// <summary>
  /// A registration request was rejected.
  /// </summary>
  RegistrationRejected = 4,
  /// <summary>
  /// The registration of a partner was removed.
  /// </summary>
  Unlinked = 5,
  /// <summary>
  /// Public keys were changed or partners were informed about new keys.
  /// </summary>
  KeysChanged = 6,
  /// <summary>
  /// A record was sent to a partner.
  /// </summary>
  RecordPushed = 7,
  /// <summary>
  /// A record of a partner was received.
  /// </summary>
  RecordReceived = 8,
  /// <summary>
  /// The metadata of a synchronized record were updated.
  /// </summary>
  RecordUpdated = 9,
  /// <summary>
  /// A synchronized record was deleted.
  /// </summary>
  RecordDeleted = 10,
  /// <summary>
  /// Records of a partner were removed after the registration was removed.
  /// </summary>
  RecordsRemoved = 11,
  /// <summary>
  /// The synchronization of an own record with a partner was revoked and the record was removed on the partner.
  /// </summary>
  RecordRevoked = 12,
  /// <summary>
  /// The address of an instance was changed or a partner was informed about the new address of this instance.
  /// </summary>
  AddressChanged = 13,
}
