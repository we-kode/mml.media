using System;
using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// Rule which allows the clients of groups to share records. Values of one criterion are combined with OR, criteria with AND.
/// </summary>
public class ShareRule
{
  /// <summary>
  /// Id of the rule.
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Groups whose clients the rule allows to share records.
  /// </summary>
  public IList<Group> Groups { get; set; } = [];

  /// <summary>
  /// True, if the rule matches all records the groups can access. The criteria are empty then.
  /// </summary>
  public bool AllRecords { get; set; }

  /// <summary>
  /// Genres the rule matches. Empty matches all genres.
  /// </summary>
  public IList<Genre> Genres { get; set; } = [];

  /// <summary>
  /// Albums the rule matches. Empty matches all albums.
  /// </summary>
  public IList<Album> Albums { get; set; } = [];

  /// <summary>
  /// Artists the rule matches. Empty matches all artists.
  /// </summary>
  public IList<Artist> Artists { get; set; } = [];
}
