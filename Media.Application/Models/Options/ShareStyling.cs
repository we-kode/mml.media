using System.Collections.Generic;

namespace Media.Application.Models.Options;

/// <summary>
/// Title and colors of the public share pages, read from the file <c>styling.json</c>. The defaults are the title and the color scheme of
/// the default mobile app, so the pages look like the app.
/// </summary>
public class ShareStyling
{
  /// <summary>
  /// The title of the app per language. The entry <c>Default</c> is used for other languages.
  /// </summary>
  public Dictionary<string, string> Title { get; set; } = new()
  {
    ["Default"] = "Meine Mediathek",
    ["de"] = "Meine Mediathek",
    ["en"] = "My Media Lib",
    ["ru"] = "Моя Медиятека",
  };

  /// <summary>
  /// The colors of the light theme.
  /// </summary>
  public ShareColors Light { get; set; } = new();

  /// <summary>
  /// The colors of the dark theme.
  /// </summary>
  public ShareColors Dark { get; set; } = new()
  {
    Surface = "#1A1C1E",
    OnSurface = "#E3E2E6",
    SurfaceContainerHighest = "#43474E",
    OnSurfaceVariant = "#C4C6CF",
    Primary = "#A8C8FF",
    OnPrimary = "#003061",
    PrimaryContainer = "#004689",
    OnPrimaryContainer = "#D5E3FF",
    Outline = "#8E9199",
  };
}
