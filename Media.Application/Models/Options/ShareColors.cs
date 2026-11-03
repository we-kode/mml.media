namespace Media.Application.Models.Options;

/// <summary>
/// Colors of a theme of the share pages as hex values, named like the roles of the color scheme of the app. The defaults are the colors
/// of the light theme of the default app.
/// </summary>
public class ShareColors
{
  /// <summary>
  /// Background of the pages.
  /// </summary>
  public string Surface { get; set; } = "#FDFBFF";

  /// <summary>
  /// Text on the background.
  /// </summary>
  public string OnSurface { get; set; } = "#1A1C1E";

  /// <summary>
  /// Background of the card.
  /// </summary>
  public string SurfaceContainerHighest { get; set; } = "#E0E2EC";

  /// <summary>
  /// Secondary text.
  /// </summary>
  public string OnSurfaceVariant { get; set; } = "#43474E";

  /// <summary>
  /// Buttons and the animation.
  /// </summary>
  public string Primary { get; set; } = "#245FA6";

  /// <summary>
  /// Text on buttons.
  /// </summary>
  public string OnPrimary { get; set; } = "#FFFFFF";

  /// <summary>
  /// Background of the record count.
  /// </summary>
  public string PrimaryContainer { get; set; } = "#D5E3FF";

  /// <summary>
  /// Text of the record count.
  /// </summary>
  public string OnPrimaryContainer { get; set; } = "#001B3C";

  /// <summary>
  /// Footer and focus frames.
  /// </summary>
  public string Outline { get; set; } = "#74777F";
}
