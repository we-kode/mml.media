using System;

namespace Media.Messages;

/// <summary>
/// Message published, when one new file was uplaoded to server
/// </summary>
public interface FileUploaded
{
  /// <summary>
  /// The name of the file, which was updated.
  /// </summary>
  string FileName { get; set; }

  /// <summary>
  /// The last modified date of the file.
  /// </summary>
  DateTime Date { get; set; }
}
