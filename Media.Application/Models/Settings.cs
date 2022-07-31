using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// Settings model for availdable settings.
/// </summary>
public class Settings
{
  /// <summary>
  /// The bitrate in kbit/s to compress the records to.
  /// </summary>
  public int? CompressionRate { get; set; }

  /// <summary>
  /// Returns the settings as Dictionary.
  /// </summary>
  /// <returns><see cref="IDictionary{TKey, TValue}"/></returns>
  public IDictionary<string, string> ToDictionary()
  {
    var dict = new Dictionary<string, string>
    {
      { nameof(CompressionRate), !CompressionRate.HasValue ? "" : CompressionRate.Value.ToString() }
    };

    return dict;
  }
}
