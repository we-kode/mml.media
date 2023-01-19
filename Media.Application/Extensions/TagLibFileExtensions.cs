using System.Linq;

namespace Media.Application.Extensions
{
  public static class TagLibFileExtensions
  {
    public static string? LanguageTag(this TagLib.File? file)
    {
      if (file == null)
      {
        return null;
      }

      var custom = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
      var languageFrame = custom.GetFrames().FirstOrDefault(frame => System.Text.Encoding.Default.GetString(frame.FrameId.Data) == "TLAN");
      if (languageFrame == null)
      {
        return null;
      }

      return custom.GetTextAsString(languageFrame.FrameId);
    }
  }
}
