using Media.Application.Contracts.Repositories;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBExclusion = Media.DBContext.Models.FederationExclusion;
using DBLink = Media.DBContext.Models.FederationRecordLink;

namespace Media.Infrastructure.Repositories;

public class SqlFederationLinkRepository(Func<ApplicationDBContext> contextFactory) : IFederationLinkRepository
{
  public RecordLink? GetByRecord(Guid peerId, Guid localRecordId)
  {
    using var context = contextFactory();
    var link = context.FederationRecordLinks.FirstOrDefault(l => l.PeerId == peerId && l.LocalRecordId == localRecordId);
    return link == null ? null : Map(link);
  }

  public RecordLink? GetByTransaction(Guid peerId, Guid transactionId)
  {
    using var context = contextFactory();
    var link = context.FederationRecordLinks.FirstOrDefault(l => l.PeerId == peerId && l.TransactionId == transactionId);
    return link == null ? null : Map(link);
  }

  public ISet<Guid> LinkedRecordIds(Guid peerId, IEnumerable<Guid> localRecordIds)
  {
    using var context = contextFactory();
    var ids = localRecordIds.ToList();
    return context.FederationRecordLinks
      .Where(l => l.PeerId == peerId && ids.Contains(l.LocalRecordId))
      .Select(l => l.LocalRecordId)
      .ToHashSet();
  }

  public async Task Create(RecordLink link)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    context.FederationRecordLinks.Add(new DBLink
    {
      PeerId = link.PeerId,
      LocalRecordId = link.LocalRecordId,
      RemoteRecordId = link.RemoteRecordId,
      Direction = (int)link.Direction,
      TransactionId = link.TransactionId,
      SyncedAt = now,
      UpdatedAt = now,
    });
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task Touch(Guid linkId)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    await context.FederationRecordLinks
      .Where(l => l.LinkId == linkId)
      .ExecuteUpdateAsync(s => s.SetProperty(l => l.UpdatedAt, now))
      .ConfigureAwait(false);
  }

  public IList<RecordLink> ListByRecord(Guid localRecordId)
  {
    using var context = contextFactory();
    return [.. context.FederationRecordLinks
      .Include(l => l.Peer)
      .Where(l => l.LocalRecordId == localRecordId)
      .OrderBy(l => l.SyncedAt)
      .AsEnumerable()
      .Select(l =>
      {
        var link = Map(l);
        link.Instance = l.Peer.Instance;
        return link;
      })];
  }

  public async Task Delete(Guid peerId, Guid localRecordId)
  {
    using var context = contextFactory();
    await context.FederationRecordLinks
      .Where(l => l.PeerId == peerId && l.LocalRecordId == localRecordId)
      .ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public ISet<Guid> ExcludedRecordIds(Guid peerId, IEnumerable<Guid> localRecordIds)
  {
    using var context = contextFactory();
    var ids = localRecordIds.ToList();
    return context.FederationExclusions
      .Where(e => e.PeerId == peerId && ids.Contains(e.RecordId))
      .Select(e => e.RecordId)
      .ToHashSet();
  }

  public async Task Exclude(Guid peerId, IEnumerable<Guid> localRecordIds)
  {
    var ids = localRecordIds.ToList();
    var excluded = ExcludedRecordIds(peerId, ids);
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    context.FederationExclusions.AddRange(ids.Distinct().Where(id => !excluded.Contains(id)).Select(id => new DBExclusion
    {
      PeerId = peerId,
      RecordId = id,
      CreatedAt = now,
    }));
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task Include(Guid peerId, IEnumerable<Guid> localRecordIds)
  {
    using var context = contextFactory();
    var ids = localRecordIds.ToList();
    await context.FederationExclusions
      .Where(e => e.PeerId == peerId && ids.Contains(e.RecordId))
      .ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public IList<RecordExclusion> ListExclusions(Guid localRecordId)
  {
    using var context = contextFactory();
    return [.. context.FederationExclusions
      .Where(e => e.RecordId == localRecordId)
      .OrderBy(e => e.CreatedAt)
      .Select(e => new RecordExclusion
      {
        PeerId = e.PeerId,
        Instance = e.Peer.Instance,
        CreatedAt = e.CreatedAt,
      })];
  }

  private static RecordLink Map(DBLink link)
  {
    return new RecordLink
    {
      LinkId = link.LinkId,
      PeerId = link.PeerId,
      LocalRecordId = link.LocalRecordId,
      RemoteRecordId = link.RemoteRecordId,
      Direction = (SyncDirection)link.Direction,
      TransactionId = link.TransactionId,
      SyncedAt = link.SyncedAt,
      UpdatedAt = link.UpdatedAt,
    };
  }
}
