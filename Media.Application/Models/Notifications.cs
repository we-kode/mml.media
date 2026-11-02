using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// Chunk of notifications.
/// </summary>
public class Notifications
{
  /// <summary>
  /// The total count of available notifications.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded notifications.
  /// </summary>
  public IList<Notification> Items { get; set; } = [];
}
