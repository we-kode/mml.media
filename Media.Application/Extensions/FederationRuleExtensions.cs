using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Application.Extensions;

/// <summary>
/// Evaluates federation rules against the metadata of a record.
/// </summary>
public static class FederationRuleExtensions
{
  /// <summary>
  /// Returns whether the rule matches the given metadata. The names of the artists, albums and genres of the rule
  /// are compared case-insensitive and trimmed with the names of the record. The groups of outgoing rules are a
  /// criterion as well, which matches records in one of the groups.
  /// </summary>
  /// <param name="rule">The rule.</param>
  /// <param name="artist">The artist of the record.</param>
  /// <param name="album">The album of the record.</param>
  /// <param name="genre">The genre of the record.</param>
  /// <param name="groupIds">The ids of the groups of the record.</param>
  /// <returns>True, if every criterion of the rule is empty or contains the value of the record.</returns>
  public static bool Matches(this Rule rule, string? artist, string? album, string? genre, IEnumerable<Guid>? groupIds = null)
  {
    return Matches(rule.Artists.Select(a => a.Name), artist)
      && Matches(rule.Albums.Select(a => a.AlbumName), album)
      && Matches(rule.Genres.Select(g => g.Name), genre)
      && (rule.Direction != SyncDirection.Outgoing || rule.Groups.Count == 0
        || rule.Groups.Any(g => groupIds?.Contains(g.Id) ?? false));
  }

  /// <summary>
  /// Returns the most specific rule matching the given metadata.
  /// Rules restricted to partners win over rules for all partners. Then rules with more criteria are more specific.
  /// On equality album, then artist, then genre criteria win, then the rule with fewer values and at last the older rule.
  /// </summary>
  /// <param name="rules">The rules to evaluate.</param>
  /// <param name="artist">The artist of the record.</param>
  /// <param name="album">The album of the record.</param>
  /// <param name="genre">The genre of the record.</param>
  /// <param name="groupIds">The ids of the groups of the record, which are needed for outgoing rules.</param>
  /// <returns>The most specific matching <see cref="Rule"/> or null if no rule matches.</returns>
  public static Rule? MostSpecific(this IEnumerable<Rule> rules, string? artist, string? album, string? genre, IEnumerable<Guid>? groupIds = null)
  {
    var groups = groupIds?.ToList();
    return rules
      .Where(rule => rule.Matches(artist, album, genre, groups))
      .OrderByDescending(rule => rule.Peers.Count > 0)
      .ThenByDescending(rule => (rule.Albums.Count > 0 ? 1 : 0) + (rule.Artists.Count > 0 ? 1 : 0) + (rule.Genres.Count > 0 ? 1 : 0))
      .ThenByDescending(rule => rule.Albums.Count > 0)
      .ThenByDescending(rule => rule.Artists.Count > 0)
      .ThenByDescending(rule => rule.Genres.Count > 0)
      .ThenBy(rule => rule.Albums.Count + rule.Artists.Count + rule.Genres.Count)
      .ThenBy(rule => rule.CreatedAt)
      .FirstOrDefault();
  }

  private static bool Matches(IEnumerable<string> criterion, string? value)
  {
    var names = criterion.ToList();
    if (names.Count == 0)
    {
      return true;
    }

    var trimmed = value?.Trim();
    return !string.IsNullOrEmpty(trimmed) && names.Any(c => string.Equals(c.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
  }
}
