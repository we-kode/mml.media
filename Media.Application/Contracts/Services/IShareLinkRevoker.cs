using Media.Application.Models;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Revokes share links of clients, which contain records their creator may not share anymore. Links of admins are never revoked.
/// </summary>
public interface IShareLinkRevoker
{
  /// <summary>
  /// Deletes the active links of clients, which contain a record their creator may not share anymore. Links of clients whose rights can
  /// not be loaded are kept.
  /// </summary>
  /// <param name="clientId">Only the links of this client are checked, all links if it is null.</param>
  Task RevokeUnallowed(string? clientId = null);

  /// <summary>
  /// Checks whether the creator of the link may still share all of its records and deletes the link otherwise.
  /// </summary>
  /// <param name="link">The link to be checked.</param>
  /// <returns>True, if the link is kept.</returns>
  Task<bool> KeepIfAllowed(ShareLink link);
}
