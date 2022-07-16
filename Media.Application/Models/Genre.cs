using System;

namespace Media.Application.Models;

public class Genre
{
  /// <summary>
  /// Id of genre.
  /// </summary>
  public Guid GenreId { get; set; }

  /// <summary>
  /// Name of genre.
  /// </summary>
  public string Name { get; set; } = String.Empty;
}
