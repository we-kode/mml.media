using Media.Application.Contracts.IO;
using MimeDetective;
using System.IO;
using System.Linq;

namespace Media.API.Services;

public class Mp3Inspector : IAudioInspector
{
  private static readonly IContentInspector inspector = new ContentInspectorBuilder()
  {
    Definitions = MimeDetective.Definitions.DefaultDefinitions.FileTypes.Audio.MP3(),
    Parallel = true
  }.Build();

  private static readonly string[] allowedMimeTypes = ["audio/mpeg3", "audio/mpeg", "audio/mp3", "audio/mpg", "audio/x-mpeg-3"];

  public bool IsMp3(Stream stream)
  {
    var results = inspector.Inspect(stream).ByMimeType();
    return results.Any(result => allowedMimeTypes.Contains(result.MimeType));
  }
}
