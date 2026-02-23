using System;
using System.Collections.Generic;

namespace Media.Messages;

/// <summary>
/// Message published, when one new file was uplaoded to server
/// </summary>
public class FileUploaded
{
  /// <summary>
  /// The name of the file, which was updated.
  /// </summary>
  public string FileName { get; set; } = string.Empty;

  /// <summary>
  /// The last modified date of the file.
  /// </summary>
  public DateTime Date { get; set; }

  /// <summary>
  /// Groups the record belongs to.
  /// </summary>
  public List<Guid> Groups { get; set; } = [];
}
