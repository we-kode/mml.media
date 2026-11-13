using Media.Application.Contracts.Repositories;
using System;

namespace Media.Infrastructure.Services;

/// <summary>
/// Protects against a changed instance name. Records, partner registrations and tokens depend on the name,
/// so the name used at the first start must not be changed afterwards.
/// </summary>
public static class InstanceNameGuard
{
  /// <summary>
  /// Key of the setting storing the name this instance was started with.
  /// </summary>
  public const string SettingKey = "InstanceName";

  /// <summary>
  /// Stores the name at the first start and ensures at every following start that it did not change.
  /// The names are compared case sensitive, since the owner of a record is compared this way.
  /// </summary>
  /// <param name="settings">The settings used to store the name.</param>
  /// <param name="instance">The configured name of this instance.</param>
  /// <exception cref="InvalidOperationException">If the instance was started with another name before.</exception>
  public static void Ensure(ISettingRepository settings, string instance)
  {
    var stored = settings.Get(SettingKey, string.Empty);
    if (string.IsNullOrEmpty(stored))
    {
      settings.Save(SettingKey, instance);
      return;
    }

    if (!string.Equals(stored, instance, StringComparison.Ordinal))
    {
      throw new InvalidOperationException(
        $"The instance name '{instance}' differs from the name '{stored}' this instance was started with. "
        + "Records, partner registrations and access tokens depend on the name, so it cannot be changed afterwards. "
        + $"Set INSTANCE back to '{stored}'. See the upgrade guide of the documentation for the steps to rename an instance on purpose.");
    }
  }
}
