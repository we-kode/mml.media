using System;
using System.Text.RegularExpressions;

namespace Media.Application.Extensions;

public static partial class CoverExtensions
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

  /// <summary>
  /// Checks whether the given name is a cover file name created by <see cref="ToFileName"/>.
  /// </summary>
  /// <param name="fileName">The name to check.</param>
  /// <returns>True if the name is a cover file name.</returns>
  public static bool IsCoverFileName(this string fileName)
  {
    return CoverFileName().IsMatch(fileName);
  }

  /// <summary>
  /// Returns the media type of the given cover or null, if the data is no supported image.
  /// Supported are JPEG and PNG images.
  /// </summary>
  /// <param name="coverData">The cover content</param>
  /// <returns>The media type or null.</returns>
  public static string? ToContentType(this byte[] coverData)
  {
    if (coverData.AsSpan().StartsWith(Jpeg))
    {
      return "image/jpeg";
    }

    return coverData.AsSpan().StartsWith(Png) ? "image/png" : null;
  }

  private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];

  private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

  [GeneratedRegex(@"^[A-Za-z0-9_-]{16}\.jpg$")]
  private static partial Regex CoverFileName();
}
