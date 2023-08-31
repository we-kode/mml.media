using System.Collections.Generic;

namespace Media.Application.Models
{
  public class Livestreams
  {
  /// <summary>
  /// The total count of available livestreams
  /// </summary>
  public int TotalCount { get; set; }

    /// <summary>
    /// On chunk of loaded livestreams.
    /// </summary>
    public IList<Livestream> Items { get; set; }

    /// <summary>
    /// Constructs new Instance
    /// </summary>
    public Livestreams()
    {
      Items = new List<Livestream>();
    }
  }
}
