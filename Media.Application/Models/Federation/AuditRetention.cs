using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// The time entries of the federation audit log are kept.
/// </summary>
public static class AuditRetention
{
  /// <summary>
  /// Key of the setting which stores the retention in days.
  /// </summary>
  public const string SettingKey = "FederationAuditRetentionDays";

  /// <summary>
  /// Retention in days used if no valid value is stored.
  /// </summary>
  public const int DefaultDays = 90;

  /// <summary>
  /// The selectable retentions in days. 0 keeps the entries until they are deleted manually.
  /// </summary>
  public static readonly IReadOnlyList<int> AllowedDays = [7, 30, 90, 180, 365, 0];

  /// <summary>
  /// Returns the retention in days of the stored setting value.
  /// </summary>
  /// <param name="value">The stored setting value.</param>
  /// <returns>The retention in days or <see cref="DefaultDays"/> if the value is missing or not selectable.</returns>
  public static int Days(string? value)
  {
    return int.TryParse(value, out var days) && ((IList<int>)AllowedDays).Contains(days) ? days : DefaultDays;
  }
}
