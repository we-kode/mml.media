using Media.Application.Types;

namespace Media.Application.Extensions;

/// <summary>
/// Extensions of federation operations.
/// </summary>
public static class FederationJobExtensions
{
  /// <summary>
  /// Returns the audit log action of the operation type.
  /// </summary>
  /// <param name="type">Type of the operation.</param>
  /// <returns>The <see cref="AuditAction"/> written to the audit log.</returns>
  public static AuditAction ToAuditAction(this FederationJobType type)
  {
    return type switch
    {
      FederationJobType.RegistrationResponse => AuditAction.RegistrationApproved,
      FederationJobType.KeysChanged => AuditAction.KeysChanged,
      FederationJobType.PushRecord => AuditAction.RecordPushed,
      FederationJobType.UpdateRecord => AuditAction.RecordUpdated,
      _ => AuditAction.RecordDeleted,
    };
  }
}
