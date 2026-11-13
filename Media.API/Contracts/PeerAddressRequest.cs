using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// The new address of a partner instance.
/// </summary>
public class PeerAddressRequest
{
  /// <summary>
  /// The new public base url of the partner.
  /// </summary>
  [Required]
  public string BaseUrl { get; set; } = string.Empty;
}
