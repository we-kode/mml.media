using System;

namespace Media.Application.Models;

public class GenreBitrate
{
  /// <summary>
  /// Id of genre.
  /// </summary>
  public Guid GenreId { get; set; }

  /// <summary>
  /// Name of genre.
  /// </summary>
  public string Name { get; set; } = String.Empty;

  /// <summary>
  /// Bitrate of genre.
  /// </summary>
  public int? Bitrate { get; set; }
}
