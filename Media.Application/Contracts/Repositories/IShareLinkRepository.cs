using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IShareLinkRepository
{
  /// <summary>
  /// Creates a link.
  /// </summary>
  /// <param name="tokenHash">Hash of the secret token of the link.</param>
  /// <param name="expiresAt">Date when the link expires.</param>
  /// <param name="singleUse">True, if the link can be used only once.</param>
  /// <param name="createdBy">Subject of the creator.</param>
  /// <param name="createdByAdmin">True, if the creator is an admin.</param>
  /// <param name="recordIds">Ids of the records of the link.</param>
  Task Create(string tokenHash, DateTime expiresAt, bool singleUse, string createdBy, bool createdByAdmin, IList<Guid> recordIds);

  /// <summary>
  /// Returns the link, if it exists, is not expired and not already used.
  /// </summary>
  /// <param name="tokenHash">Hash of the secret token of the link.</param>
  /// <param name="now">Current date.</param>
  ShareLink? Find(string tokenHash, DateTime now);

  /// <summary>
  /// Marks a link as used. Only one caller can use a single use link.
  /// </summary>
  /// <param name="id">Id of the link.</param>
  /// <param name="now">Current date.</param>
  /// <returns>True, if the link may be used.</returns>
  Task<bool> TryUse(Guid id, DateTime now);

  /// <summary>
  /// Returns the active links of a creator, the newest first.
  /// </summary>
  /// <param name="createdBy">Subject of the creator.</param>
  /// <param name="now">Current date.</param>
  IList<ShareLinkInfo> GetActive(string createdBy, DateTime now);

  /// <summary>
  /// Deletes a link of a creator, so it can not be used anymore.
  /// </summary>
  /// <param name="id">Id of the link.</param>
  /// <param name="createdBy">Subject of the creator.</param>
  /// <returns>True, if the link of the creator existed.</returns>
  Task<bool> Delete(Guid id, string createdBy);

  /// <summary>
  /// Returns the active links created by clients.
  /// </summary>
  /// <param name="createdBy">Subject of the client or null for the links of all clients.</param>
  /// <param name="now">Current date.</param>
  IList<ShareLink> GetActiveOfClients(string? createdBy, DateTime now);

  /// <summary>
  /// Deletes links, so they can not be used anymore.
  /// </summary>
  /// <param name="ids">Ids of the links.</param>
  Task Revoke(IList<Guid> ids);

  /// <summary>
  /// Removes expired links and used single use links.
  /// </summary>
  /// <param name="now">Current date.</param>
  Task RemoveExpired(DateTime now);
}
