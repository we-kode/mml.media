using System.IO;

namespace Media.Application.Models;

/// <summary>
/// Contains file to be streamed
/// </summary>
/// <remarks>
/// Initializes the record stream.
/// </remarks>
/// <param name="mimeType">The mime type of the record.</param>
/// <param name="stream">The file stream of the record.</param>
public class RecordStream(string mimeType, FileStream stream)
{
  /// <summary>
  /// The mime type of the file
  /// </summary>
  public string MimeType { get; set; } = mimeType;

  /// <summary>
  /// The stream of the record.
  /// </summary>
  public FileStream Stream { get; set; } = stream;
}
