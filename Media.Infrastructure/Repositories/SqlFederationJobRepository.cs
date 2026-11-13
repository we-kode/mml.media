using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Extensions;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBAuditEntry = Media.DBContext.Models.FederationAuditEntry;
using DBJob = Media.DBContext.Models.FederationJob;

namespace Media.Infrastructure.Repositories;

public class SqlFederationJobRepository(Func<ApplicationDBContext> contextFactory) : IFederationJobRepository
{
  public async Task Enqueue(Guid peerId, FederationJobType type, Guid? recordId = null, Guid? transactionId = null)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    context.FederationJobs.Add(new DBJob
    {
      PeerId = peerId,
      Type = (int)type,
      RecordId = recordId,
      TransactionId = transactionId,
      NextAttemptAt = now,
      CreatedAt = now,
    });
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<IList<AuditEntry>> EnqueueForOutgoingLinks(Guid recordId, FederationJobType type)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    var links = context.FederationRecordLinks
      .Where(l => l.LocalRecordId == recordId && l.Direction == (int)SyncDirection.Outgoing)
      .Where(l => type != FederationJobType.UpdateRecord
        || !context.FederationJobs.Any(j => !j.Failed && j.PeerId == l.PeerId && j.RecordId == recordId && j.Type == (int)FederationJobType.UpdateRecord))
      .Select(l => new { l.PeerId, l.Peer.Instance, l.TransactionId })
      .ToList();

    var peerIds = links.Select(l => l.PeerId).ToList();
    await context.FederationJobs
      .Where(j => j.Failed && j.RecordId == recordId && j.Type == (int)type && peerIds.Contains(j.PeerId))
      .ExecuteDeleteAsync().ConfigureAwait(false);

    var entries = new List<DBAuditEntry>();
    context.FederationJobs.AddRange(links.Select(l => new DBJob
    {
      PeerId = l.PeerId,
      Type = (int)type,
      RecordId = recordId,
      TransactionId = l.TransactionId,
      NextAttemptAt = now,
      CreatedAt = now,
      AuditEntryId = AddPending(context, entries, type, l.Instance, l.TransactionId, recordId, now),
    }));
    await context.SaveChangesAsync().ConfigureAwait(false);
    return [.. entries.Select(SqlFederationAuditRepository.Map)];
  }

  public async Task<IList<AuditEntry>> EnqueueRange(IEnumerable<FederationJob> jobs)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    var list = jobs.ToList();
    var peerIds = list.Select(j => j.PeerId).Distinct().ToList();
    var instances = context.FederationPeers
      .Where(p => peerIds.Contains(p.PeerId))
      .ToDictionary(p => p.PeerId, p => p.Instance);

    // a new operation replaces the failed one for the same record and partner.
    foreach (var group in list.Where(j => j.RecordId.HasValue).GroupBy(j => (j.PeerId, j.Type)))
    {
      var recordIds = group.Select(j => j.RecordId!.Value).ToList();
      await context.FederationJobs
        .Where(j => j.Failed && j.PeerId == group.Key.PeerId && j.Type == (int)group.Key.Type && j.RecordId.HasValue && recordIds.Contains(j.RecordId.Value))
        .ExecuteDeleteAsync().ConfigureAwait(false);
    }

    var entries = new List<DBAuditEntry>();
    context.FederationJobs.AddRange(list.Select(j => new DBJob
    {
      PeerId = j.PeerId,
      Type = (int)j.Type,
      RecordId = j.RecordId,
      TransactionId = j.TransactionId,
      NextAttemptAt = now,
      CreatedAt = now,
      AuditEntryId = j.RecordId.HasValue && instances.TryGetValue(j.PeerId, out var instance)
        ? AddPending(context, entries, j.Type, instance, j.TransactionId, j.RecordId.Value, now)
        : null,
    }));
    await context.SaveChangesAsync().ConfigureAwait(false);
    return [.. entries.Select(SqlFederationAuditRepository.Map)];
  }

  // the audit log shows the operation as pending until it is finished.
  private static Guid AddPending(ApplicationDBContext context, List<DBAuditEntry> entries, FederationJobType type, string instance, Guid? transactionId, Guid recordId, DateTime now)
  {
    var entry = new DBAuditEntry
    {
      EntryId = Guid.NewGuid(),
      Timestamp = now,
      Action = (int)type.ToAuditAction(),
      Status = (int)AuditStatus.Pending,
      Source = Env.INSTANCE,
      Target = instance,
      TransactionId = transactionId,
      RecordIds = [recordId],
    };
    context.FederationAuditLog.Add(entry);
    entries.Add(entry);
    return entry.EntryId;
  }

  public ISet<Guid> PendingRecordIds(Guid peerId, FederationJobType type, IEnumerable<Guid> recordIds)
  {
    using var context = contextFactory();
    var ids = recordIds.ToList();
    return context.FederationJobs
      .Where(j => !j.Failed && j.PeerId == peerId && j.Type == (int)type && j.RecordId.HasValue && ids.Contains(j.RecordId.Value))
      .Select(j => j.RecordId!.Value)
      .ToHashSet();
  }

  public IList<FederationJob> Due(DateTime now, int take)
  {
    using var context = contextFactory();
    return [.. context.FederationJobs
      .Where(j => !j.Failed && j.NextAttemptAt <= now)
      .OrderBy(j => j.CreatedAt)
      .Take(take)
      .Select(j => new FederationJob
      {
        JobId = j.JobId,
        PeerId = j.PeerId,
        Type = (FederationJobType)j.Type,
        RecordId = j.RecordId,
        TransactionId = j.TransactionId,
        Attempts = j.Attempts,
        AuditEntryId = j.AuditEntryId,
      })];
  }

  public async Task Complete(Guid jobId)
  {
    using var context = contextFactory();
    await context.FederationJobs.Where(j => j.JobId == jobId).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public async Task Retry(Guid jobId, string error, DateTime nextAttemptAt)
  {
    using var context = contextFactory();
    await context.FederationJobs
      .Where(j => j.JobId == jobId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(j => j.Attempts, j => j.Attempts + 1)
        .SetProperty(j => j.LastError, error)
        .SetProperty(j => j.NextAttemptAt, nextAttemptAt))
      .ConfigureAwait(false);
  }

  public async Task Fail(Guid jobId, string error)
  {
    using var context = contextFactory();
    await context.FederationJobs
      .Where(j => j.JobId == jobId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(j => j.Attempts, j => j.Attempts + 1)
        .SetProperty(j => j.LastError, error)
        .SetProperty(j => j.Failed, true))
      .ConfigureAwait(false);
  }

  public IList<RecordSyncProblem> ListProblems(Guid recordId)
  {
    using var context = contextFactory();
    return [.. context.FederationJobs
      .Where(j => j.RecordId == recordId && (j.Failed || j.LastError != null))
      .OrderBy(j => j.CreatedAt)
      .Select(j => new RecordSyncProblem
      {
        Instance = j.Peer.Instance,
        Type = (FederationJobType)j.Type,
        Error = j.LastError,
        Failed = j.Failed,
        NextAttemptAt = j.Failed ? null : j.NextAttemptAt,
      })];
  }

  public async Task<IList<(Guid RecordId, Guid? AuditEntryId)>> CancelPush(Guid peerId, IEnumerable<Guid> recordIds)
  {
    using var context = contextFactory();
    var ids = recordIds.ToList();
    var jobs = context.FederationJobs
      .Where(j => j.PeerId == peerId && j.Type == (int)FederationJobType.PushRecord && j.RecordId.HasValue && ids.Contains(j.RecordId.Value));
    var cancelled = jobs.Select(j => new { RecordId = j.RecordId!.Value, j.AuditEntryId }).AsEnumerable().Select(j => (j.RecordId, j.AuditEntryId)).ToList();
    await jobs.ExecuteDeleteAsync().ConfigureAwait(false);
    return cancelled;
  }

  public async Task Reschedule(Guid peerId, DateTime now)
  {
    using var context = contextFactory();
    await context.FederationJobs
      .Where(j => j.PeerId == peerId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(j => j.Failed, false)
        .SetProperty(j => j.Attempts, 0)
        .SetProperty(j => j.NextAttemptAt, now))
      .ConfigureAwait(false);
  }

  public async Task SetProgress(Guid jobId, long transferred, long size)
  {
    using var context = contextFactory();
    await context.FederationJobs
      .Where(j => j.JobId == jobId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(j => j.Transferred, transferred)
        .SetProperty(j => j.Size, size))
      .ConfigureAwait(false);
  }

  public IDictionary<Guid, (long Transferred, long Size)> Progress(IEnumerable<Guid> entryIds)
  {
    using var context = contextFactory();
    var ids = entryIds.ToList();
    return context.FederationJobs
      .Where(j => !j.Failed && j.Size.HasValue && j.AuditEntryId.HasValue && ids.Contains(j.AuditEntryId.Value))
      .Select(j => new { EntryId = j.AuditEntryId!.Value, j.Transferred, Size = j.Size!.Value })
      .ToDictionary(j => j.EntryId, j => (j.Transferred, j.Size));
  }

  public IList<RecordSyncTransfer> ListTransfers(Guid recordId)
  {
    using var context = contextFactory();
    return [.. context.FederationJobs
      .Where(j => j.RecordId == recordId && !j.Failed && j.Type == (int)FederationJobType.PushRecord)
      .OrderBy(j => j.CreatedAt)
      .Select(j => new RecordSyncTransfer
      {
        Instance = j.Peer.Instance,
        Transferred = j.Transferred,
        Size = j.Size,
        NextAttemptAt = j.NextAttemptAt,
      })];
  }
}
