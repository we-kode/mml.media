using System.IO;

namespace Media.Application.Contracts.IO;

/// <summary>
/// Detects the format of uploaded audio content.
/// </summary>
public interface IAudioInspector
{
  /// <summary>
  /// Returns whether the content of the stream is an mp3 file.
  /// </summary>
  /// <param name="stream">The content to inspect.</param>
  /// <returns>True, if the content is an mp3 file.</returns>
  bool IsMp3(Stream stream);
}
