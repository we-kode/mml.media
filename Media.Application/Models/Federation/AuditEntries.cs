using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A list of audit log entries.
/// </summary>
public class AuditEntries
{
  /// <summary>
  /// The total count of available entries.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded entries.
  /// </summary>
  public IList<AuditEntry> Items { get; set; } = [];
}
