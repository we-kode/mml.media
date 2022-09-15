using System;

namespace Media.Application.Models;

public class Album
{
  /// <summary>
  /// Id of album.
  /// </summary>
  public Guid AlbumId { get; set; }

  /// <summary>
  /// Name of album.
  /// </summary>
  public string AlbumName { get; set; } = String.Empty;
}
