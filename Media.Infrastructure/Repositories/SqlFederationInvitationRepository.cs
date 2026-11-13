using Media.Application.Contracts.Repositories;
using Media.Application.Models.Federation;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DBInvitation = Media.DBContext.Models.FederationInvitation;

namespace Media.Infrastructure.Repositories;

public class SqlFederationInvitationRepository(Func<ApplicationDBContext> contextFactory) : IFederationInvitationRepository
{
  public Invitations ListOpen(DateTime now)
  {
    using var context = contextFactory();
    var items = context.FederationInvitations
      .Where(i => i.UsedAt == null && i.ExpiresAt > now)
      .OrderBy(i => i.ExpiresAt)
      .AsEnumerable()
      .Select(Map)
      .ToList();
    return new Invitations { TotalCount = items.Count, Items = items };
  }

  public async Task<Invitation> Create(string codeHash, DateTime expiresAt)
  {
    using var context = contextFactory();
    var invitation = new DBInvitation
    {
      CodeHash = codeHash,
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = expiresAt,
    };
    context.FederationInvitations.Add(invitation);
    await context.SaveChangesAsync().ConfigureAwait(false);
    return Map(invitation);
  }

  public bool IsOpen(string codeHash, DateTime now)
  {
    using var context = contextFactory();
    return context.FederationInvitations.Any(i => i.CodeHash == codeHash && i.UsedAt == null && i.ExpiresAt > now);
  }

  public async Task<bool> Redeem(string codeHash, DateTime now, string usedBy)
  {
    using var context = contextFactory();
    var updated = await context.FederationInvitations
      .Where(i => i.CodeHash == codeHash && i.UsedAt == null && i.ExpiresAt > now)
      .ExecuteUpdateAsync(s => s
        .SetProperty(i => i.UsedAt, now)
        .SetProperty(i => i.UsedBy, usedBy))
      .ConfigureAwait(false);
    return updated == 1;
  }

  public async Task Delete(Guid invitationId)
  {
    using var context = contextFactory();
    await context.FederationInvitations.Where(i => i.InvitationId == invitationId).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public async Task DeleteClosed(DateTime now)
  {
    using var context = contextFactory();
    await context.FederationInvitations.Where(i => i.UsedAt != null || i.ExpiresAt <= now).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  private static Invitation Map(DBInvitation invitation)
  {
    return new Invitation
    {
      InvitationId = invitation.InvitationId,
      CreatedAt = invitation.CreatedAt,
      ExpiresAt = invitation.ExpiresAt,
    };
  }
}
