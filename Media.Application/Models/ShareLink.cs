using System;
using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// A link which allows anonymous download of records.
/// </summary>
public class ShareLink
{
  /// <summary>
  /// Id of the link.
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// True, if the link can be used only once.
  /// </summary>
  public bool SingleUse { get; set; }

  /// <summary>
  /// Date when the link expires.
  /// </summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// Subject of the admin or client, who created the link.
  /// </summary>
  public string CreatedBy { get; set; } = string.Empty;

  /// <summary>
  /// True, if an admin created the link. Links of admins are never revoked.
  /// </summary>
  public bool CreatedByAdmin { get; set; }

  /// <summary>
  /// Ids of the records of the link.
  /// </summary>
  public IList<Guid> RecordIds { get; set; } = [];
}
