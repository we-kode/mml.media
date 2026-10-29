namespace Media.Application.Types;

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
