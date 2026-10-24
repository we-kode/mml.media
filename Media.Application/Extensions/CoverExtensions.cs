namespace Media.Application.Extensions;

public static class CoverExtensions
{
  /// <summary>
  /// Creates a file name based on the content of the cover. This is used to avoid storing duplicate covers and to be able to easily retrieve the cover file based on the content.
  /// </summary>
  /// <param name="coverData">The cover content</param>
  /// <returns>The new filename of the cover.</returns>
  public static string ToFileName(this byte[] coverData)
  {
    var hash = System.Security.Cryptography.SHA256.HashData(coverData);
    string fileName = System.Buffers.Text.Base64Url.EncodeToString(hash)[..16];
    return $"{fileName}.jpg";
  }
}
