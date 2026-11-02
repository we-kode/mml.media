using Media.Application.Models;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Creates or changes a notification.
/// </summary>
public class NotificationChangeRequest
{
  /// <summary>
  /// Id of the notification to change or null to create a new one.
  /// </summary>
  public Guid? NotificationId { get; set; }

  /// <summary>
  /// Title of the notification.
  /// </summary>
  [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
  [StringLength(200)]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Plain text message of the notification.
  /// </summary>
  [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
  public string Message { get; set; } = string.Empty;

  /// <summary>
  /// Severity of the notification.
  /// </summary>
  [EnumDataType(typeof(NotificationSeverity))]
  public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

  /// <summary>
  /// Start of the validity or null, if the notification is valid immediately.
  /// </summary>
  public DateTime? ValidFrom { get; set; }

  /// <summary>
  /// End of the validity or null, if the notification is valid until it is deleted.
  /// </summary>
  public DateTime? ValidTo { get; set; }

  /// <summary>
  /// Ids of the clients the notification is shown to.
  /// </summary>
  public ICollection<string> ClientIds { get; set; } = [];

  /// <summary>
  /// Groups whose clients the notification is shown to. Without groups and clients it is shown to all clients.
  /// </summary>
  public ICollection<Group> Groups { get; set; } = [];
}
