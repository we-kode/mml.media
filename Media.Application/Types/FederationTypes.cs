namespace Media.Application.Types;

/// <summary>
/// Registration states of a partner instance.
/// </summary>
public enum PeerState
{
  /// <summary>
  /// This instance requested the registration and waits for the answer of the partner.
  /// </summary>
  Requested = 1,

  /// <summary>
  /// The partner requested the registration and waits for the approval of an admin.
  /// </summary>
  PendingApproval = 2,

  /// <summary>
  /// The registration was approved, but the partner was not informed yet.
  /// </summary>
  ResponsePending = 3,

  /// <summary>
  /// Both instances are linked and records are synchronized.
  /// </summary>
  Linked = 4,

  /// <summary>
  /// The partner rejected the registration.
  /// </summary>
  Rejected = 5,
}

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
}

/// <summary>
/// Results of an action logged in the federation audit log.
/// </summary>
public enum AuditStatus
{
  /// <summary>
  /// The action succeeded.
  /// </summary>
  Success = 1,
  /// <summary>
  /// The action failed.
  /// </summary>
  Failed = 2,
  /// <summary>
  /// The action was not necessary.
  /// </summary>
  Skipped = 3,
  /// <summary>
  /// The action is scheduled and not finished yet.
  /// </summary>
  Pending = 4,
}

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
}
