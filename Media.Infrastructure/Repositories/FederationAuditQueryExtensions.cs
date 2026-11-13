using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using DBAuditEntry = Media.DBContext.Models.FederationAuditEntry;

namespace Media.Infrastructure.Repositories;

/// <summary>
/// Applies an <see cref="AuditQuery"/> to the audit log.
/// </summary>
public static class FederationAuditQueryExtensions
{
  /// <summary>
  /// Filters the entries by the criteria of the query. The direction and the partner instance
  /// are determined by comparing source and target with the name of this instance.
  /// </summary>
  /// <param name="entries">The audit log entries.</param>
  /// <param name="query">Filter of the entries.</param>
  /// <param name="localInstance">Name of this instance.</param>
  /// <returns>The filtered entries.</returns>
  [SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons",
    Justification = "The expression is translated to SQL by EF Core, which does not support the StringComparison overloads.")]
  public static IQueryable<DBAuditEntry> Filter(this IQueryable<DBAuditEntry> entries, AuditQuery query, string localInstance)
  {
    var statuses = query.Statuses.Select(s => (int)s).Distinct().ToList();
    if (statuses.Count > 0)
    {
      entries = entries.Where(a => statuses.Contains(a.Status));
    }

    var actions = query.Actions.Select(a => (int)a).Distinct().ToList();
    if (actions.Count > 0)
    {
      entries = entries.Where(a => actions.Contains(a.Action));
    }

    if (query.Direction == SyncDirection.Outgoing)
    {
      entries = entries.Where(a => a.Source == localInstance);
    }
    else if (query.Direction == SyncDirection.Incoming)
    {
      entries = entries.Where(a => a.Source != localInstance);
    }

    var instances = query.Instances.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
    if (instances.Count > 0)
    {
      entries = entries.Where(a => instances.Contains(a.Source == localInstance ? a.Target : a.Source));
    }

    if (query.From.HasValue)
    {
      var from = query.From.Value;
      entries = entries.Where(a => a.Timestamp >= from);
    }

    if (query.To.HasValue)
    {
      var to = query.To.Value;
      entries = entries.Where(a => a.Timestamp < to);
    }

    if (!string.IsNullOrWhiteSpace(query.Message))
    {
      var message = query.Message.Trim().ToLower();
      entries = entries.Where(a => a.Message != null && a.Message.ToLower().Contains(message));
    }

    if (!string.IsNullOrWhiteSpace(query.Filter))
    {
      var filter = query.Filter.Trim().ToLower();
      entries = entries.Where(a => a.Source.ToLower().Contains(filter)
        || a.Target.ToLower().Contains(filter)
        || (a.Message != null && a.Message.ToLower().Contains(filter)));
    }

    return entries;
  }

  /// <summary>
  /// Sorts the entries by the columns of the query in order of priority. Entries with equal values
  /// are sorted newest first.
  /// </summary>
  /// <param name="entries">The audit log entries.</param>
  /// <param name="query">Sort order of the entries.</param>
  /// <param name="localInstance">Name of this instance.</param>
  /// <returns>The sorted entries.</returns>
  public static IOrderedQueryable<DBAuditEntry> Sort(this IQueryable<DBAuditEntry> entries, AuditQuery query, string localInstance)
  {
    IOrderedQueryable<DBAuditEntry>? sorted = null;
    foreach (var sort in query.Sort)
    {
      sorted = sort.Field switch
      {
        AuditSortField.Status => entries.OrderOrThenBy(sorted, a => a.Status, sort.Descending),
        AuditSortField.Action => entries.OrderOrThenBy(sorted, a => a.Action, sort.Descending),
        AuditSortField.Direction => entries.OrderOrThenBy(sorted, a => a.Source == localInstance, sort.Descending),
        AuditSortField.Instance => entries.OrderOrThenBy(sorted, a => a.Source == localInstance ? a.Target : a.Source, sort.Descending),
        _ => entries.OrderOrThenBy(sorted, a => a.Timestamp, sort.Descending),
      };
    }

    return entries.OrderOrThenBy(sorted, a => a.Timestamp, true).ThenBy(a => a.EntryId);
  }

  private static IOrderedQueryable<DBAuditEntry> OrderOrThenBy<TKey>(this IQueryable<DBAuditEntry> entries, IOrderedQueryable<DBAuditEntry>? sorted, Expression<Func<DBAuditEntry, TKey>> key, bool descending)
  {
    if (sorted == null)
    {
      return descending ? entries.OrderByDescending(key) : entries.OrderBy(key);
    }
    return descending ? sorted.ThenByDescending(key) : sorted.ThenBy(key);
  }
}
