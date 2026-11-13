using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBShareLink = Media.DBContext.Models.ShareLink;

namespace Media.Infrastructure.Repositories;

public class SqlShareLinkRepository(Func<ApplicationDBContext> contextFactory) : IShareLinkRepository
{
  public async Task Create(string tokenHash, DateTime expiresAt, bool singleUse, string createdBy, bool createdByAdmin, IList<Guid> recordIds)
  {
    using var context = contextFactory();
    context.ShareLinks.Add(new DBShareLink
    {
      TokenHash = tokenHash,
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = expiresAt,
      SingleUse = singleUse,
      CreatedBy = createdBy,
      CreatedByAdmin = createdByAdmin,
      RecordIds = [.. recordIds],
    });
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public ShareLink? Find(string tokenHash, DateTime now)
  {
    using var context = contextFactory();
    var link = context.ShareLinks.FirstOrDefault(l => l.TokenHash == tokenHash && l.ExpiresAt > now && l.UsedAt == null);
    return link == null ? null : Map(link);
  }

  public async Task<bool> TryUse(Guid id, DateTime now)
  {
    using var context = contextFactory();
    var links = context.ShareLinks.Where(l => l.ShareLinkId == id && l.ExpiresAt > now && l.UsedAt == null);
    var link = await links.Select(l => new { l.SingleUse }).FirstOrDefaultAsync().ConfigureAwait(false);
    if (link == null)
    {
      return false;
    }

    if (!link.SingleUse)
    {
      return true;
    }

    return await links.ExecuteUpdateAsync(s => s.SetProperty(l => l.UsedAt, now)).ConfigureAwait(false) == 1;
  }

  public IList<ShareLinkInfo> GetActive(string createdBy, DateTime now)
  {
    using var context = contextFactory();
    return [.. context.ShareLinks
      .Where(l => l.CreatedBy == createdBy && l.ExpiresAt > now && l.UsedAt == null)
      .OrderByDescending(l => l.CreatedAt)
      .Select(l => new ShareLinkInfo(l.ShareLinkId, l.CreatedAt, l.ExpiresAt, l.SingleUse, l.RecordIds.Count))];
  }

  public async Task<bool> Delete(Guid id, string createdBy)
  {
    using var context = contextFactory();
    return await context.ShareLinks
      .Where(l => l.ShareLinkId == id && l.CreatedBy == createdBy)
      .ExecuteDeleteAsync()
      .ConfigureAwait(false) == 1;
  }

  public IList<ShareLink> GetActiveOfClients(string? createdBy, DateTime now)
  {
    using var context = contextFactory();
    return [.. context.ShareLinks
      .Where(l => !l.CreatedByAdmin && (createdBy == null || l.CreatedBy == createdBy) && l.ExpiresAt > now && l.UsedAt == null)
      .AsEnumerable()
      .Select(Map)];
  }

  public async Task Revoke(IList<Guid> ids)
  {
    using var context = contextFactory();
    await context.ShareLinks
      .Where(l => ids.Contains(l.ShareLinkId))
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  public async Task RemoveExpired(DateTime now)
  {
    using var context = contextFactory();
    await context.ShareLinks
      .Where(l => l.ExpiresAt <= now || l.UsedAt != null)
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  private static ShareLink Map(DBShareLink link)
  {
    return new ShareLink
    {
      Id = link.ShareLinkId,
      SingleUse = link.SingleUse,
      ExpiresAt = link.ExpiresAt,
      CreatedBy = link.CreatedBy,
      CreatedByAdmin = link.CreatedByAdmin,
      RecordIds = link.RecordIds,
    };
  }
}
