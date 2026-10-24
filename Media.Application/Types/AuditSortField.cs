namespace Media.Application.Types;

/// <summary>
/// Columns the audit log can be sorted by.
/// </summary>
public enum AuditSortField
{
  /// <summary>
  /// The creation date.
  /// </summary>
  Timestamp = 1,
  /// <summary>
  /// The result of the action.
  /// </summary>
  Status = 2,
  /// <summary>
  /// The direction seen from this instance.
  /// </summary>
  Direction = 3,
  /// <summary>
  /// The performed action.
  /// </summary>
  Action = 4,
  /// <summary>
  /// The partner instance.
  /// </summary>
  Instance = 5,
}
