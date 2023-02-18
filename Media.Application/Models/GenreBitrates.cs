using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// List of genre bitrates
/// </summary>
public class GenreBitrates
{
  /// <summary>
  /// The total count of available bitrate genres
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded bitrate genres.
  /// </summary>
  public IList<GenreBitrate> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public GenreBitrates() => Items = new List<GenreBitrate>();
}
