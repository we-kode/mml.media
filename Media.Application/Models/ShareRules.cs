using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// A list of share rules.
/// </summary>
public class ShareRules
{
  /// <summary>
  /// The total count of available rules.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded rules.
  /// </summary>
  public IList<ShareRule> Items { get; set; } = [];
}
