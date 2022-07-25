using System.Collections.Generic;

namespace Media.Application.Models;

public class Albums
{
  /// <summary>
  /// The total count of available albums
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded albums.
  /// </summary>
  public IList<Album> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public Albums() => Items = new List<Album>();
}
