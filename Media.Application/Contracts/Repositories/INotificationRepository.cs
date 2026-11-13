using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

/// <summary>
/// Stores the notifications of the admins.
/// </summary>
public interface INotificationRepository
{
  /// <summary>
  /// Loads a chunk of notifications ordered by the last change.
  /// </summary>
  /// <param name="filter">Filter of the title or message.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of the chunk.</param>
  /// <returns>The chunk of notifications.</returns>
  Notifications List(string? filter, int skip, int take);

  /// <summary>
  /// Loads the notification with the given id.
  /// </summary>
  /// <param name="id">Id of the notification.</param>
  /// <returns>The notification or null, if it does not exist.</returns>
  Notification? Load(Guid id);

  /// <summary>
  /// Checks whether the notification exists.
  /// </summary>
  /// <param name="id">Id of the notification.</param>
  /// <returns>True, if the notification exists.</returns>
  bool Exists(Guid id);

  /// <summary>
  /// Creates or updates the notification.
  /// </summary>
  /// <param name="notification">The notification to store.</param>
  Task Save(Notification notification);

  /// <summary>
  /// Deletes the notifications with the given ids.
  /// </summary>
  /// <param name="ids">Ids of the notifications.</param>
  Task Delete(IEnumerable<Guid> ids);

  /// <summary>
  /// Loads the notifications valid at the given time for the client with the given id and groups,
  /// ordered by severity and start of the validity.
  /// </summary>
  /// <param name="clientId">Id of the client.</param>
  /// <param name="clientGroups">Groups of the client.</param>
  /// <param name="now">The time the notifications must be valid.</param>
  /// <returns>The active notifications.</returns>
  IList<Notification> Active(string clientId, IList<Guid> clientGroups, DateTime now);

  /// <summary>
  /// Deletes all notifications, whose validity ended before the given time.
  /// </summary>
  /// <param name="now">The current time.</param>
  /// <returns>The number of deleted notifications.</returns>
  Task<int> RemoveExpired(DateTime now);

  /// <summary>
  /// Deletes all notifications, which are only shown to the clients of the given group, so they are not shown
  /// to all clients after the group is deleted.
  /// </summary>
  /// <param name="groupId">Id of the group to be deleted.</param>
  Task RemoveTargetingOnly(Guid groupId);
}
