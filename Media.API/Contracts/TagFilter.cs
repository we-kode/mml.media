using System;
using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Includes ids of tags for which the list should be filtered.
/// </summary>
public class TagFilter
{
  /// <summary>
  /// List of artist ids.
  /// </summary>
  public IList<Guid> Artists { get; set; }

  /// <summary>
  /// List of artist names.
  /// </summary>
  public IList<string> ArtistNames { get; set; }

  /// <summary>
  /// List of genre ids.
  /// </summary>
  public IList<Guid> Genres { get; set; }

  /// <summary>
  /// List of genre names.
  /// </summary>
  public IList<string> GenreNames { get; set; }

  /// <summary>
  /// List of album ids.
  /// </summary>
  public IList<Guid> Albums { get; set; }

  /// <summary>
  /// List of album names.
  /// </summary>
  public IList<string> AlbumNames { get; set; }

  /// <summary>
  /// List of language ids.
  /// </summary>
  public IList<Guid> Languages { get; set; }

  /// <summary>
  /// List of group ids.
  /// </summary>
  public IList<Guid> Groups { get; set; }

  /// <summary>
  /// Date filter start date.
  /// </summary>
  public DateTime? StartDate { get; set; }

  /// <summary>
  /// Date filter end date.
  /// </summary>
  public DateTime? EndDate { get; set; }

  public TagFilter()
  {
    Artists = [];
    ArtistNames = [];
    Genres = [];
    GenreNames = [];
    Albums = [];
    AlbumNames = [];
    Languages = [];
    Groups = [];
  }
}
