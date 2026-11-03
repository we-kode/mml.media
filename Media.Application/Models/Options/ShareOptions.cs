using System.ComponentModel.DataAnnotations;

namespace Media.Application.Models.Options;

/// <summary>
/// Options of sharing, which are configured in the section <c>Share</c> of the appsettings.
/// </summary>
public class ShareOptions
{
  /// <summary>
  /// Name of the section in the appsettings.
  /// </summary>
  public const string Section = "Share";

  /// <summary>
  /// Largest number of records in one link.
  /// </summary>
  [Range(1, 10000)]
  public int MaxRecordsPerLink { get; set; } = 200;

  /// <summary>
  /// Path of the styling file, which contains title and colors of the share pages.
  /// </summary>
  public string StylingPath { get; set; } = "/configs/styling.json";
}
