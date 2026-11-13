using Media.Application.Models.Options;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Media.API.Extensions;

/// <summary>
/// Loads the <see cref="ShareStyling"/> of the share pages.
/// </summary>
public static partial class ShareStylingExtensions
{
  /// <summary>
  /// Reads the styling file of the options. Missing values and colors which are no hex values are replaced by the defaults. Without a
  /// readable file the defaults are used.
  /// </summary>
  /// <param name="options">The options of sharing.</param>
  /// <returns>The styling of the share pages.</returns>
  public static ShareStyling LoadShareStyling(this ShareOptions options)
  {
    var defaults = new ShareStyling();
    var styling = new ShareStyling();
    try
    {
      var file = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(options.StylingPath), optional: true).Build();
      file.GetSection("colors:light").Bind(styling.Light);
      file.GetSection("colors:dark").Bind(styling.Dark);
      foreach (var title in file.GetSection("title").GetChildren().Where(t => !string.IsNullOrWhiteSpace(t.Value)))
      {
        styling.Title[title.Key.Equals("default", StringComparison.OrdinalIgnoreCase) ? "Default" : title.Key] = title.Value!.Trim();
      }
    }
    catch (Exception e) when (e is InvalidDataException or FormatException or IOException or UnauthorizedAccessException or InvalidOperationException)
    {
      Console.WriteLine($"The styling file '{options.StylingPath}' can not be read, the defaults are used: {e.Message}");
      return defaults;
    }

    ReplaceInvalid(styling.Light, defaults.Light);
    ReplaceInvalid(styling.Dark, defaults.Dark);
    return styling;
  }

  private static void ReplaceInvalid(ShareColors colors, ShareColors defaults)
  {
    foreach (var color in typeof(ShareColors).GetProperties().Where(p => !HexColor().IsMatch((string?)p.GetValue(colors) ?? string.Empty)))
    {
      color.SetValue(colors, color.GetValue(defaults));
    }
  }

  [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
  private static partial Regex HexColor();
}
