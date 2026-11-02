namespace Media.Application.Types;

/// <summary>
/// Severity of a notification shown to the clients.
/// </summary>
public enum NotificationSeverity
{
  /// <summary>
  /// General information.
  /// </summary>
  Info = 1,
  /// <summary>
  /// Announcement of a maintenance.
  /// </summary>
  Maintenance = 2,
  /// <summary>
  /// Important message.
  /// </summary>
  Important = 3,
}
