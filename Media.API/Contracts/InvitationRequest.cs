using Media.Application.Models.Federation;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Request to create an invitation for a partner.
/// </summary>
public class InvitationRequest
{
  /// <summary>
  /// Hours the invitation stays valid, at most 90 days.
  /// </summary>
  [Range(1, Invitation.MaxValidityHours)]
  public int ValidityHours { get; set; } = Invitation.DefaultValidityHours;
}
