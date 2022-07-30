using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// Settings model for availdable settings.
/// </summary>
public class Settings
{
  /// <summary>
  /// The compression rate in kbit/s to render records to this compression rate.
  /// </summary>
  public int? CompressionRate { get; set; }

  /// <summary>
  /// Returns the settings as Dictionary.
  /// </summary>
  /// <returns><see cref="IDictionary{TKey, TValue}"/></returns>
  public IDictionary<string, string> ToDictionary()
  {
    var dict = new Dictionary<string, string>();
    if (CompressionRate.HasValue)
    {
      dict.Add(nameof(CompressionRate), CompressionRate.Value.ToString());
    }

    return dict;
  }
}
