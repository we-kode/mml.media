using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBAuditEntry = Media.DBContext.Models.FederationAuditEntry;

namespace Media.Infrastructure.Repositories;

public class SqlFederationAuditRepository(Func<ApplicationDBContext> contextFactory) : IFederationAuditRepository
{
  public AuditEntries List(AuditQuery query, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    var instance = Env.INSTANCE;
    using var context = contextFactory();
    var entries = context.FederationAuditLog.Filter(query, instance);

    var count = entries.Count();
    var items = entries.Sort(query, instance).Skip(skip).Take(take == -1 ? count : take).AsEnumerable().Select(e => Map(e, instance)).ToList();
    AddRecordTitles(context, items);
    return new AuditEntries
    {
      TotalCount = count,
      Items = items,
    };
  }

  // the titles of all records of the page are loaded with one query. Removed records have no title.
  private static void AddRecordTitles(ApplicationDBContext context, IList<AuditEntry> entries)
  {
    var ids = entries.SelectMany(e => e.RecordIds).Distinct().ToList();
    if (ids.Count == 0)
    {
      return;
    }

    var titles = context.Records
      .Where(r => ids.Contains(r.RecordId))
      .Select(r => new { r.RecordId, r.Title })
      .ToDictionary(r => r.RecordId, r => r.Title);
    foreach (var entry in entries)
    {
      entry.Records = [.. entry.RecordIds.Where(titles.ContainsKey).Select(id => new AuditRecord(id, titles[id]))];
    }
  }

  public IList<string> ListInstances()
  {
    var instance = Env.INSTANCE;
    using var context = contextFactory();
    return [.. context.FederationAuditLog
      .Select(a => a.Source == instance ? a.Target : a.Source)
      .Distinct()
      .OrderBy(i => i)];
  }

  public async Task<AuditEntry> Add(AuditEntry entry)
  {
    using var context = contextFactory();
    var dbEntry = new DBAuditEntry
    {
      Timestamp = DateTime.UtcNow,
      Action = (int)entry.Action,
      Status = (int)entry.Status,
      Source = entry.Source,
      Target = entry.Target,
      TransactionId = entry.TransactionId,
      RecordIds = [.. entry.RecordIds],
      Message = entry.Message,
    };
    context.FederationAuditLog.Add(dbEntry);
    await context.SaveChangesAsync().ConfigureAwait(false);
    return Map(dbEntry);
  }

  public Guid? FindPending(AuditAction action, string source, Guid transactionId)
  {
    using var context = contextFactory();
    return context.FederationAuditLog
      .Where(e => e.Action == (int)action && e.Source == source && e.TransactionId == transactionId && e.Status == (int)AuditStatus.Pending)
      .Select(e => (Guid?)e.EntryId)
      .FirstOrDefault();
  }

  public async Task Delete(IEnumerable<Guid> entryIds)
  {
    using var context = contextFactory();
    var ids = entryIds.ToList();
    await context.FederationAuditLog.Where(a => ids.Contains(a.EntryId)).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public async Task<int> Delete(AuditQuery query)
  {
    using var context = contextFactory();
    return await context.FederationAuditLog.Filter(query, Env.INSTANCE).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public async Task<(AuditEntry Entry, AuditStatus Previous)?> Update(Guid entryId, AuditStatus status, string? message, bool onlyPending = false)
  {
    using var context = contextFactory();
    var entry = context.FederationAuditLog.FirstOrDefault(e => e.EntryId == entryId);
    if (entry == null || (onlyPending && entry.Status != (int)AuditStatus.Pending))
    {
      return null;
    }

    var previous = (AuditStatus)entry.Status;
    entry.Status = (int)status;
    entry.Message = message;
    await context.SaveChangesAsync().ConfigureAwait(false);
    return (Map(entry), previous);
  }

  public async Task<int> DeleteOlderThan(DateTime before)
  {
    using var context = contextFactory();
    return await context.FederationAuditLog.Where(e => e.Timestamp < before).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Maps the stored entry to the audit log entry seen from this instance.
  /// </summary>
  /// <param name="entry">The stored entry.</param>
  /// <returns>The <see cref="AuditEntry"/>.</returns>
  public static AuditEntry Map(DBAuditEntry entry) => Map(entry, Env.INSTANCE);

  private static AuditEntry Map(DBAuditEntry entry, string instance)
  {
    var outgoing = entry.Source == instance;
    return new AuditEntry
    {
      EntryId = entry.EntryId,
      Timestamp = entry.Timestamp,
      Action = (AuditAction)entry.Action,
      Status = (AuditStatus)entry.Status,
      Source = entry.Source,
      Target = entry.Target,
      Direction = outgoing ? SyncDirection.Outgoing : SyncDirection.Incoming,
      Instance = outgoing ? entry.Target : entry.Source,
      TransactionId = entry.TransactionId,
      RecordIds = entry.RecordIds,
      Message = entry.Message,
    };
  }
}
