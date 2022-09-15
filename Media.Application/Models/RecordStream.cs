using System.IO;

namespace Media.Application.Models;

/// <summary>
/// Contains file to be streamed
/// </summary>
public class RecordStream
{
  /// <summary>
  /// The mime type of the file
  /// </summary>
  public string MimeType { get; set; }

  /// <summary>
  /// The stream of the record.
  /// </summary>
  public FileStream Stream { get; set; }

  /// <summary>
  /// Initializes the record stream.
  /// </summary>
  /// <param name="mimeType">The mime type of the record.</param>
  /// <param name="stream">The file stream of the record.</param>
  public RecordStream(string mimeType, FileStream stream)
  {
    MimeType = mimeType;
    Stream = stream;
  }
}
