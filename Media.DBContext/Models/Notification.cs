using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Media.DBContext.Models;

/// <summary>
/// Message of the admins shown to the clients, e.g. an announcement of a maintenance.
/// </summary>
[Table("notifications")]
public class Notification
{
  /// <summary>
  /// Primary Key.
  /// </summary>
  [Key]
  public Guid NotificationId { get; set; }

  /// <summary>
  /// Title of the notification.
  /// </summary>
  [Required]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Plain text message of the notification.
  /// </summary>
  [Required]
  public string Message { get; set; } = string.Empty;

  /// <summary>
  /// Severity of the notification.
  /// </summary>
  public int Severity { get; set; }

  /// <summary>
  /// Start of the validity or null, if the notification is valid immediately.
  /// </summary>
  public DateTime? ValidFrom { get; set; }

  /// <summary>
  /// End of the validity or null, if the notification is valid until it is deleted.
  /// </summary>
  public DateTime? ValidTo { get; set; }

  /// <summary>
  /// Time of the last change.
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Ids of the clients the notification is shown to.
  /// </summary>
  public List<string> ClientIds { get; set; } = [];

  /// <summary>
  /// Groups whose clients the notification is shown to. Without groups and clients it is shown to all clients.
  /// </summary>
  public ICollection<Group> Groups { get; set; } = [];
}
