using Media.Application.Models.Options;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Settings which control the links clients create.
/// </summary>
public class ShareSettingsRequest
{
  /// <summary>
  /// True, if clients choose validity and usage of their links themselves.
  /// </summary>
  public bool UserSettingsAllowed { get; set; }

  /// <summary>
  /// Validity in hours of links of clients, if they cannot choose.
  /// </summary>
  [Range(1, ShareSettings.MaxValidForHours)]
  public int UserValidForHours { get; set; } = ShareSettings.MaxValidForHours;

  /// <summary>
  /// True, if links of clients are single use links, if they cannot choose.
  /// </summary>
  public bool UserSingleUse { get; set; } = true;

  /// <summary>
  /// Days a shared search is kept after it was opened the last time.
  /// </summary>
  [Range(1, 3650)]
  public int SearchLinkRetentionDays { get; set; } = 30;
}
