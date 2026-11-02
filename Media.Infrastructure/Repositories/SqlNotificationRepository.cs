using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure.Repositories;

public class SqlNotificationRepository(Func<ApplicationDBContext> contextFactory) : INotificationRepository
{
  public Notifications List(string? filter, int skip, int take)
  {
    using var context = contextFactory();
    var query = context.Notifications
      .Where(n => string.IsNullOrEmpty(filter) || EF.Functions.ILike(n.Title, $"%{filter}%") || EF.Functions.ILike(n.Message, $"%{filter}%"));

    return new Notifications
    {
      TotalCount = query.Count(),
      Items = [.. query
        .Include(n => n.Groups)
        .OrderByDescending(n => n.UpdatedAt)
        .Skip(skip)
        .Take(take)
        .AsEnumerable()
        .Select(Map)],
    };
  }

  public Notification? Load(Guid id)
  {
    using var context = contextFactory();
    var item = context.Notifications.Include(n => n.Groups).FirstOrDefault(n => n.NotificationId == id);
    return item == null ? null : Map(item);
  }

  public bool Exists(Guid id)
  {
    using var context = contextFactory();
    return context.Notifications.Any(n => n.NotificationId == id);
  }

  public async Task Save(Notification notification)
  {
    using var context = contextFactory();
    var item = context.Notifications.Include(n => n.Groups).FirstOrDefault(n => n.NotificationId == notification.NotificationId);
    if (item == null)
    {
      item = new DBContext.Models.Notification { NotificationId = notification.NotificationId };
      context.Notifications.Add(item);
    }

    var groupIds = notification.Groups.Select(g => g.Id).ToList();
    item.Title = notification.Title;
    item.Message = notification.Message;
    item.Severity = (int)notification.Severity;
    item.ValidFrom = notification.ValidFrom?.ToUniversalTime();
    item.ValidTo = notification.ValidTo?.ToUniversalTime();
    item.UpdatedAt = DateTime.UtcNow;
    item.ClientIds = [.. notification.ClientIds.Distinct()];
    item.Groups = [.. context.Groups.Where(g => groupIds.Contains(g.GroupId))];
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task Delete(IEnumerable<Guid> ids)
  {
    using var context = contextFactory();
    await context.Notifications.Where(n => ids.Contains(n.NotificationId)).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public IList<Notification> Active(string clientId, IList<Guid> clientGroups, DateTime now)
  {
    using var context = contextFactory();
    return [.. context.Notifications
      .Where(n => (n.ValidFrom == null || n.ValidFrom <= now) && (n.ValidTo == null || n.ValidTo >= now))
      .Where(n => (!n.Groups.Any() && n.ClientIds.Count == 0)
        || n.ClientIds.Contains(clientId)
        || n.Groups.Any(g => clientGroups.Contains(g.GroupId)))
      .OrderByDescending(n => n.Severity)
      .ThenBy(n => n.ValidFrom ?? n.UpdatedAt)
      .AsEnumerable()
      .Select(Map)];
  }

  public async Task<int> RemoveExpired(DateTime now)
  {
    using var context = contextFactory();
    return await context.Notifications.Where(n => n.ValidTo != null && n.ValidTo < now).ExecuteDeleteAsync().ConfigureAwait(false);
  }

  public async Task RemoveTargetingOnly(Guid groupId)
  {
    using var context = contextFactory();
    await context.Notifications
      .Where(n => n.ClientIds.Count == 0 && n.Groups.Any(g => g.GroupId == groupId) && n.Groups.All(g => g.GroupId == groupId))
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  private static Notification Map(DBContext.Models.Notification item)
  {
    return new Notification
    {
      NotificationId = item.NotificationId,
      Title = item.Title,
      Message = item.Message,
      Severity = (NotificationSeverity)item.Severity,
      ValidFrom = item.ValidFrom,
      ValidTo = item.ValidTo,
      UpdatedAt = item.UpdatedAt,
      ClientIds = item.ClientIds,
      Groups = [.. item.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault))],
    };
  }
}
