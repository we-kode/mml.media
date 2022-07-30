using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Request to store settings.
/// </summary>
public class SettingsRequest
{
  /// <summary>
  /// The compression rate of recordss in kbit/s.
  /// </summary>
  [Range(1, int.MaxValue, ErrorMessageResourceName = nameof(Resources.ValidationMessages.MinValue), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
  public int? CompressionRate { get; set; }
}
