using System.Collections.Generic;

namespace Media.Application.Models
{
  /// <summary>
  /// List of languages
  /// </summary>
  public class Languages
  {
    /// <summary>
    /// The total count of available languages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// On chunk of loaded languages.
    /// </summary>
    public IList<Language> Items { get; set; }

    /// <summary>
    /// Constructs new Instance
    /// </summary>
    public Languages() => Items = new List<Language>();
  }
}
