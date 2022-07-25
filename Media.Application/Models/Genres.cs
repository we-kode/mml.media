using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// List of genres
/// </summary>
public class Genres
{
  /// <summary>
  /// The total count of available genres
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded genres.
  /// </summary>
  public IList<Genre> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public Genres() => Items = new List<Genre>();
}
