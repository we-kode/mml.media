using Media.Application.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Media.API.Share;

/// <summary>
/// Creates file names for downloaded records.
/// </summary>
public static class ShareFileNames
{
  // most file systems allow 255 bytes per file name.
  private const int MaxBytes = 255;

  // characters invalid on any system, so the archive can be extracted everywhere and no name becomes a path.
  private static readonly char[] Invalid = [.. Path.GetInvalidFileNameChars().Union(@"\/:*?""<>|")];

  /// <summary>
  /// Returns the file extension for a mime type.
  /// </summary>
  /// <param name="mimeType">Mime type of the record.</param>
  /// <returns>Extension with leading dot.</returns>
  public static string Extension(string mimeType)
  {
    return mimeType.ToLowerInvariant() switch
    {
      "audio/mp4" or "audio/aac" or "audio/x-m4a" => ".m4a",
      "audio/ogg" => ".ogg",
      "audio/flac" or "audio/x-flac" => ".flac",
      "audio/wav" or "audio/x-wav" => ".wav",
      _ => ".mp3",
    };
  }

  /// <summary>
  /// Creates a valid, unique file name for the file of a record.
  /// </summary>
  /// <param name="stream">The opened file of the record.</param>
  /// <param name="used">File names which are already used. The returned name is added.</param>
  /// <returns>File name without path.</returns>
  public static string Create(RecordStream stream, ISet<string> used)
  {
    return Create(stream.FileName, Extension(stream.MimeType), used);
  }

  /// <summary>
  /// Creates a valid, unique file name. Names longer than file systems allow are shortened.
  /// </summary>
  /// <param name="name">Name of the file without extension.</param>
  /// <param name="extension">Extension with leading dot.</param>
  /// <param name="used">File names which are already used. The returned name is added.</param>
  /// <returns>File name without path.</returns>
  public static string Create(string? name, string extension, ISet<string> used)
  {
    var valid = new string([.. (name ?? string.Empty).Where(c => !Invalid.Contains(c) && !char.IsControl(c))]).Trim().TrimEnd('.');
    valid = valid.Length == 0 ? "record" : valid;

    var fileName = Shorten(valid, extension);
    for (var i = 2; !used.Add(fileName.ToLowerInvariant()); i++)
    {
      fileName = Shorten(valid, $" ({i}){extension}");
    }

    return fileName;
  }

  private static string Shorten(string name, string suffix)
  {
    var maxBytes = MaxBytes - Encoding.UTF8.GetByteCount(suffix);
    var elements = StringInfo.GetTextElementEnumerator(name);
    var result = new StringBuilder();
    var bytes = 0;
    while (elements.MoveNext())
    {
      var element = elements.GetTextElement();
      bytes += Encoding.UTF8.GetByteCount(element);
      if (bytes > maxBytes)
      {
        break;
      }

      result.Append(element);
    }

    return result.ToString().TrimEnd() + suffix;
  }
}
