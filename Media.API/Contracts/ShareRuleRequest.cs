using System;
using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Rule which allows the clients of groups to share records. It matches all records or at least one criterion must be set. A rule without
/// group allows nobody to share.
/// </summary>
public class ShareRuleRequest
{
  /// <summary>
  /// Id of the rule to be changed or null for a new rule.
  /// </summary>
  public Guid? Id { get; set; }

  /// <summary>
  /// Ids of the groups whose clients may share the matching records.
  /// </summary>
  public IList<Guid> Groups { get; set; } = [];

  /// <summary>
  /// True, if the rule matches all records the groups can access. The criteria are ignored then.
  /// </summary>
  public bool AllRecords { get; set; }

  /// <summary>
  /// Ids of the genres the rule matches.
  /// </summary>
  public IList<Guid> Genres { get; set; } = [];

  /// <summary>
  /// Ids of the albums the rule matches.
  /// </summary>
  public IList<Guid> Albums { get; set; } = [];

  /// <summary>
  /// Ids of the artists the rule matches.
  /// </summary>
  public IList<Guid> Artists { get; set; } = [];
}
