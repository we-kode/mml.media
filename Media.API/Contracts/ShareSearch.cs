using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Filters of a shared search, as they are set in the app.
/// </summary>
public class ShareSearch
{
  /// <summary>
  /// Text filter of the search.
  /// </summary>
  public string? Q { get; set; }

  /// <summary>
  /// Ids of the filtered artists.
  /// </summary>
  public List<string> Artist { get; set; } = [];

  /// <summary>
  /// Names of the filtered artists.
  /// </summary>
  public List<string> ArtistName { get; set; } = [];

  /// <summary>
  /// Ids of the filtered genres.
  /// </summary>
  public List<string> Genre { get; set; } = [];

  /// <summary>
  /// Names of the filtered genres.
  /// </summary>
  public List<string> GenreName { get; set; } = [];

  /// <summary>
  /// Ids of the filtered albums.
  /// </summary>
  public List<string> Album { get; set; } = [];

  /// <summary>
  /// Names of the filtered albums.
  /// </summary>
  public List<string> AlbumName { get; set; } = [];

  /// <summary>
  /// Ids of the filtered languages.
  /// </summary>
  public List<string> Language { get; set; } = [];

  /// <summary>
  /// Start date of the filtered period.
  /// </summary>
  public string? From { get; set; }

  /// <summary>
  /// End date of the filtered period.
  /// </summary>
  public string? To { get; set; }
}
