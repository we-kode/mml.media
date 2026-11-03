namespace Media.Application.Models.Options;

/// <summary>
/// Settings which control the links clients create and how long shared searches are kept.
/// </summary>
public class ShareSettings
{
  /// <summary>
  /// Longest validity of a link in hours.
  /// </summary>
  public const int MaxValidForHours = 90 * 24;

  /// <summary>
  /// True, if clients choose validity and usage of their links themselves.
  /// </summary>
  public bool UserSettingsAllowed { get; set; }

  /// <summary>
  /// Validity in hours of links of clients, if they cannot choose.
  /// </summary>
  public int UserValidForHours { get; set; } = MaxValidForHours;

  /// <summary>
  /// True, if links of clients are single use links, if they cannot choose.
  /// </summary>
  public bool UserSingleUse { get; set; } = true;

  /// <summary>
  /// Days a shared search is kept after it was opened the last time.
  /// </summary>
  public int SearchLinkRetentionDays { get; set; } = 30;
}
