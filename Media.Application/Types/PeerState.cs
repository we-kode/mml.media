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
