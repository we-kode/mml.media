using Media.Application.Types;
using System;
using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// Filter and sort order of the federation audit log. Empty criteria are ignored.
/// </summary>
public class AuditQuery
{
  /// <summary>
  /// Filters by instance name or message.
  /// </summary>
  public string? Filter { get; set; }

  /// <summary>
  /// Only entries with one of these statuses are loaded if not empty.
  /// </summary>
  public IList<AuditStatus> Statuses { get; set; } = [];

  /// <summary>
  /// Only entries with one of these actions are loaded if not empty.
  /// </summary>
  public IList<AuditAction> Actions { get; set; } = [];

  /// <summary>
  /// Only entries of this direction seen from this instance are loaded if set.
  /// </summary>
  public SyncDirection? Direction { get; set; }

  /// <summary>
  /// Only entries concerning one of these partner instances are loaded if not empty.
  /// </summary>
  public IList<string> Instances { get; set; } = [];

  /// <summary>
  /// Only entries created at or after this date are loaded if set.
  /// </summary>
  public DateTime? From { get; set; }

  /// <summary>
  /// Only entries created before this date are loaded if set.
  /// </summary>
  public DateTime? To { get; set; }

  /// <summary>
  /// Only entries whose message contains this text are loaded if set.
  /// </summary>
  public string? Message { get; set; }

  /// <summary>
  /// The columns the entries are sorted by in order of priority. Entries are sorted newest first if empty.
  /// </summary>
  public IList<AuditSort> Sort { get; set; } = [];
}

/// <summary>
/// One column the audit log is sorted by.
/// </summary>
/// <param name="Field">The column.</param>
/// <param name="Descending">Sorts the column descending if true.</param>
public record AuditSort(AuditSortField Field, bool Descending);
