using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Request to create or change a federation rule. Values of one criterion are combined with OR, criteria with AND.
/// </summary>
public class RuleRequest
{
  /// <summary>
  /// The direction the rule is applied to.
  /// </summary>
  [Required]
  [EnumDataType(typeof(SyncDirection))]
  public SyncDirection Direction { get; set; }

  /// <summary>
  /// Ids of the partners the rule is restricted to. Empty applies to all partners.
  /// </summary>
  public IList<Guid> Peers { get; set; } = [];

  /// <summary>
  /// Ids of the artists the rule matches. Empty matches all artists.
  /// </summary>
  public IList<Guid> Artists { get; set; } = [];

  /// <summary>
  /// Ids of the albums the rule matches. Empty matches all albums.
  /// </summary>
  public IList<Guid> Albums { get; set; } = [];

  /// <summary>
  /// Ids of the genres the rule matches. Empty matches all genres.
  /// </summary>
  public IList<Guid> Genres { get; set; } = [];

  /// <summary>
  /// Incoming rules: ids of the groups received records are assigned to.
  /// Outgoing rules: only records in one of these groups match, empty matches all records.
  /// </summary>
  public IList<Guid> Groups { get; set; } = [];
}
