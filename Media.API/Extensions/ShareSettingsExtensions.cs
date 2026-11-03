using Media.Application.Contracts.Repositories;
using Media.Application.Models.Options;

namespace Media.API.Extensions;

/// <summary>
/// Loads and saves the <see cref="ShareSettings"/> in the settings of the server.
/// </summary>
public static class ShareSettingsExtensions
{
  private const string UserSettingsAllowedKey = "Share.UserSettingsAllowed";
  private const string UserValidForHoursKey = "Share.UserValidForHours";
  private const string UserSingleUseKey = "Share.UserSingleUse";
  private const string SearchLinkRetentionDaysKey = "Share.SearchLinkRetentionDays";

  /// <summary>
  /// Loads the share settings. Missing values are replaced by the defaults.
  /// </summary>
  /// <param name="repository">Repository of the settings.</param>
  /// <returns>The share settings.</returns>
  public static ShareSettings LoadShareSettings(this ISettingRepository repository)
  {
    var defaults = new ShareSettings();
    return new ShareSettings
    {
      UserSettingsAllowed = bool.TryParse(repository.Get(UserSettingsAllowedKey, ""), out var allowed) && allowed,
      UserValidForHours = int.TryParse(repository.Get(UserValidForHoursKey, ""), out var hours) ? hours : defaults.UserValidForHours,
      UserSingleUse = !bool.TryParse(repository.Get(UserSingleUseKey, ""), out var singleUse) || singleUse,
      SearchLinkRetentionDays = int.TryParse(repository.Get(SearchLinkRetentionDaysKey, ""), out var days) && days > 0
        ? days
        : defaults.SearchLinkRetentionDays,
    };
  }

  /// <summary>
  /// Saves the share settings.
  /// </summary>
  /// <param name="repository">Repository of the settings.</param>
  /// <param name="settings">The settings to be saved.</param>
  public static void SaveShareSettings(this ISettingRepository repository, ShareSettings settings)
  {
    repository.Save(UserSettingsAllowedKey, settings.UserSettingsAllowed.ToString());
    repository.Save(UserValidForHoursKey, settings.UserValidForHours.ToString());
    repository.Save(UserSingleUseKey, settings.UserSingleUse.ToString());
    repository.Save(SearchLinkRetentionDaysKey, settings.SearchLinkRetentionDays.ToString());
  }
}
