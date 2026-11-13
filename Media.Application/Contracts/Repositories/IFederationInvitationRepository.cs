using Media.Application.Models.Federation;
using System;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationInvitationRepository
{
  /// <summary>
  /// Returns all invitations which are neither used nor expired.
  /// </summary>
  /// <param name="now">The current date.</param>
  /// <returns><see cref="Invitations"/></returns>
  Invitations ListOpen(DateTime now);

  /// <summary>
  /// Stores a new invitation.
  /// </summary>
  /// <param name="codeHash">The hash of the code.</param>
  /// <param name="expiresAt">Date when the invitation expires.</param>
  /// <returns>The stored <see cref="Invitation"/> without code.</returns>
  Task<Invitation> Create(string codeHash, DateTime expiresAt);

  /// <summary>
  /// Returns whether an open invitation with the given code hash exists.
  /// </summary>
  /// <param name="codeHash">The hash of the code.</param>
  /// <param name="now">The current date.</param>
  /// <returns>True, if the invitation can be used.</returns>
  bool IsOpen(string codeHash, DateTime now);

  /// <summary>
  /// Marks an open invitation as used. Each invitation can only be used once.
  /// </summary>
  /// <param name="codeHash">The hash of the code.</param>
  /// <param name="now">The current date.</param>
  /// <param name="usedBy">The instance name of the partner.</param>
  /// <returns>True, if the invitation was open and is used now.</returns>
  Task<bool> Redeem(string codeHash, DateTime now, string usedBy);

  /// <summary>
  /// Removes one invitation.
  /// </summary>
  /// <param name="invitationId">Id of the invitation.</param>
  Task Delete(Guid invitationId);

  /// <summary>
  /// Removes all invitations which are used or expired.
  /// </summary>
  /// <param name="now">The current date.</param>
  Task DeleteClosed(DateTime now);
}
