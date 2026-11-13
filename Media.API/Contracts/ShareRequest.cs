using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace Media.API.Contracts;

/// <summary>
/// Request to create a share link.
/// </summary>
public class ShareRequest
{
  /// <summary>
  /// Ids of the records to be shared.
  /// </summary>
  [MaxLength(5000)]
  public IList<Guid> Records { get; set; } = [];

  /// <summary>
  /// Folders, whose records are to be shared.
  /// </summary>
  [MaxLength(1000)]
  public IList<RecordFolder> Folders { get; set; } = [];

  /// <summary>
  /// Validity of the link in hours. Defaults to the longest validity.
  /// </summary>
  public int? ValidForHours { get; set; }

  /// <summary>
  /// True, if the link can be used only once. Defaults to true.
  /// </summary>
  public bool? SingleUse { get; set; }
}
