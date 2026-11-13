using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Request to register at a partner instance.
/// </summary>
public class PeerRegistrationRequest
{
  /// <summary>
  /// The public https base url of the partner.
  /// </summary>
  [Required]
  [MaxLength(2048)]
  public string BaseUrl { get; set; } = string.Empty;

  /// <summary>
  /// The invitation code created by the admin of the partner.
  /// </summary>
  [Required]
  [MaxLength(64)]
  public string InvitationCode { get; set; } = string.Empty;
}
