using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A list of rules.
/// </summary>
public class Rules
{
  /// <summary>
  /// The total count of rules.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// The loaded rules.
  /// </summary>
  public IList<Rule> Items { get; set; } = [];
}
