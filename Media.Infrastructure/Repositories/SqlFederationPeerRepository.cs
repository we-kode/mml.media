using Media.Application.Contracts.Repositories;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DBPeer = Media.DBContext.Models.FederationPeer;

namespace Media.Infrastructure.Repositories;

public class SqlFederationPeerRepository(Func<ApplicationDBContext> contextFactory) : IFederationPeerRepository
{
  public Peers List(string? filter, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = contextFactory();
    var query = context.FederationPeers
      .Where(p => string.IsNullOrEmpty(filter)
        || EF.Functions.ILike(p.Instance, $"%{filter}%")
        || EF.Functions.ILike(p.BaseUrl, $"%{filter}%"))
      .OrderBy(p => p.Instance);

    var count = query.Count();
    return new Peers
    {
      TotalCount = count,
      Items = [.. query.Skip(skip).Take(take == -1 ? count : take).AsEnumerable().Select(Map)],
    };
  }

  public IList<Peer> ListByState(params PeerState[] states)
  {
    using var context = contextFactory();
    var values = states.Select(s => (int)s).ToList();
    return [.. context.FederationPeers.Where(p => values.Contains(p.State)).AsEnumerable().Select(Map)];
  }

  public Peer? Get(Guid peerId)
  {
    using var context = contextFactory();
    var peer = context.FederationPeers.FirstOrDefault(p => p.PeerId == peerId);
    return peer == null ? null : Map(peer);
  }

  [SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons",
    Justification = "The expression is translated to SQL by EF Core, which does not support the StringComparison overloads.")]
  public Peer? GetByInstance(string instance)
  {
    using var context = contextFactory();
    var name = instance.ToLower();
    var peer = context.FederationPeers.FirstOrDefault(p => p.Instance.ToLower() == name);
    return peer == null ? null : Map(peer);
  }

  public async Task<Peer> Create(Peer peer)
  {
    using var context = contextFactory();
    var now = DateTime.UtcNow;
    var dbPeer = new DBPeer
    {
      Instance = peer.Instance,
      BaseUrl = peer.BaseUrl,
      Endpoint = peer.Endpoint,
      JwksUri = peer.JwksUri,
      Jwks = peer.Jwks,
      JwksUpdatedAt = peer.JwksUpdatedAt,
      State = (int)peer.State,
      RequestId = peer.RequestId,
      CreatedAt = now,
      UpdatedAt = now,
    };
    context.FederationPeers.Add(dbPeer);
    await context.SaveChangesAsync().ConfigureAwait(false);
    return Map(dbPeer);
  }

  public async Task Update(Peer peer)
  {
    using var context = contextFactory();
    var dbPeer = context.FederationPeers.FirstOrDefault(p => p.PeerId == peer.PeerId);
    if (dbPeer == null)
    {
      return;
    }

    dbPeer.BaseUrl = peer.BaseUrl;
    dbPeer.Endpoint = peer.Endpoint;
    dbPeer.JwksUri = peer.JwksUri;
    dbPeer.Jwks = peer.Jwks;
    dbPeer.JwksUpdatedAt = peer.JwksUpdatedAt;
    dbPeer.State = (int)peer.State;
    dbPeer.RequestId = peer.RequestId;
    dbPeer.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task Delete(Guid peerId)
  {
    using var context = contextFactory();

    // rules restricted to this partner only would apply to all partners without it, so they are removed.
    await context.FederationRules
      .Where(r => r.Peers.Any(p => p.PeerId == peerId) && r.Peers.All(p => p.PeerId == peerId))
      .ExecuteDeleteAsync().ConfigureAwait(false);

    // the operations are removed together with the partner, so their entries are not pending anymore.
    // transfers received from the partner are removed as well.
    var instance = context.FederationPeers.Where(p => p.PeerId == peerId).Select(p => p.Instance).FirstOrDefault();
    var entryIds = context.FederationJobs.Where(j => j.PeerId == peerId && j.AuditEntryId.HasValue).Select(j => j.AuditEntryId!.Value).ToList();
    await context.FederationAuditLog
      .Where(e => (entryIds.Contains(e.EntryId) || (e.Source == instance && e.Action == (int)AuditAction.RecordReceived)) && e.Status == (int)AuditStatus.Pending)
      .ExecuteUpdateAsync(s => s
        .SetProperty(e => e.Status, (int)AuditStatus.Skipped)
        .SetProperty(e => e.Message, "The partner was removed."))
      .ConfigureAwait(false);
    await context.FederationPeers.Where(p => p.PeerId == peerId).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  private static Peer Map(DBPeer peer)
  {
    return new Peer
    {
      PeerId = peer.PeerId,
      Instance = peer.Instance,
      BaseUrl = peer.BaseUrl,
      Endpoint = peer.Endpoint,
      JwksUri = peer.JwksUri,
      Jwks = peer.Jwks,
      JwksUpdatedAt = peer.JwksUpdatedAt,
      State = (PeerState)peer.State,
      RequestId = peer.RequestId,
      CreatedAt = peer.CreatedAt,
      UpdatedAt = peer.UpdatedAt,
    };
  }
}
