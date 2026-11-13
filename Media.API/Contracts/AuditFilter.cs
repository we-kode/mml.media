using Media.Application.Types;
using System;
using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Filter and sort order of the federation audit log. Empty criteria are ignored.
/// </summary>
public class AuditFilter
{
  /// <summary>
  /// Filters by instance name or message.
  /// </summary>
  public string? Filter { get; set; }

  /// <summary>
  /// Filters by status. Several statuses can be passed.
  /// </summary>
  public IList<AuditStatus>? Status { get; set; }

  /// <summary>
  /// Filters by action. Several actions can be passed.
  /// </summary>
  public IList<AuditAction>? Action { get; set; }

  /// <summary>
  /// Filters by the direction seen from this instance.
  /// </summary>
  public SyncDirection? Direction { get; set; }

  /// <summary>
  /// Filters by partner instance. Several instances can be passed.
  /// </summary>
  public IList<string>? Instance { get; set; }

  /// <summary>
  /// Loads entries created at or after this date.
  /// </summary>
  public DateTimeOffset? From { get; set; }

  /// <summary>
  /// Loads entries created before this date.
  /// </summary>
  public DateTimeOffset? To { get; set; }

  /// <summary>
  /// Filters by text contained in the message.
  /// </summary>
  public string? Message { get; set; }

  /// <summary>
  /// The columns the entries are sorted by in order of priority, see <see cref="AuditSortField"/>.
  /// A leading <c>-</c> sorts the column descending, e.g. <c>-timestamp</c>. Unknown columns are ignored.
  /// Entries are sorted newest first without sort columns.
  /// </summary>
  public IList<string>? Sort { get; set; }
}
