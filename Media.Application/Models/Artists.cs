using System.Collections.Generic;

namespace Media.Application.Models;

public class Artists
{
  /// <summary>
  /// The total count of available artists
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded artists.
  /// </summary>
  public IList<Artist> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public Artists() => Items = new List<Artist>();
}
