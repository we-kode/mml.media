namespace Media.Application.Contracts;

public interface ISettingsRepository
{

  /// <summary>
  /// Returns saved value of given key.
  /// </summary>
  /// <param name="key">key of the setting to be loaded.</param>
  /// <param name="defaultValue">Default Value if setting not exists.</param>
  /// <returns>Saved value as <see cref="string"/>.</returns>
  string Get(string key, string defaultValue);

  /// <summary>
  /// Saves the given settings.
  /// </summary>
  /// <param name="key">Key to be saved.</param>
  /// <param name="value">Value to be saved.</param>
  void Save(string key, string value);
}
