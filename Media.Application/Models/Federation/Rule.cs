using Media.Application.Types;
using System;
using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A filter rule which determines the records synchronized with partners. The rule applies to all partners or to the partners it is restricted to.
/// Values of one criterion are combined with OR, criteria are combined with AND, an empty criterion matches all records.
/// </summary>
public class Rule
{
  /// <summary>
  /// Id of the rule.
  /// </summary>
  public Guid RuleId { get; set; }

  /// <summary>
  /// The partners the rule is restricted to. Empty applies to all partners.
  /// </summary>
  public IList<Peer> Peers { get; set; } = [];

  /// <summary>
  /// The direction the rule is applied to.
  /// </summary>
  public SyncDirection Direction { get; set; }

  /// <summary>
  /// Artists the rule matches.
  /// </summary>
  public IList<Artist> Artists { get; set; } = [];

  /// <summary>
  /// Albums the rule matches.
  /// </summary>
  public IList<Album> Albums { get; set; } = [];

  /// <summary>
  /// Genres the rule matches.
  /// </summary>
  public IList<Genre> Genres { get; set; } = [];

  /// <summary>
  /// For incoming rules the groups received records matching the rule are assigned to.
  /// For outgoing rules only records in one of these groups match, empty matches all records.
  /// </summary>
  public IList<Group> Groups { get; set; } = [];

  /// <summary>
  /// Date when the rule was created.
  /// </summary>
  public DateTime CreatedAt { get; set; }
}
