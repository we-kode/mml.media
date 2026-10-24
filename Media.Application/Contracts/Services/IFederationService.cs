using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Manages the registration of partner instances.
/// </summary>
public interface IFederationService
{
  /// <summary>
  /// Returns the public information of this instance.
  /// </summary>
  /// <returns><see cref="FederationDiscovery"/></returns>
  /// <exception cref="FederationException">If the federation is disabled.</exception>
  FederationDiscovery GetDiscovery();

  /// <summary>
  /// Creates a one-time invitation a partner must present to request a registration at this instance.
  /// </summary>
  /// <returns>The <see cref="Invitation"/> including the code.</returns>
  /// <exception cref="FederationException">If the federation is disabled.</exception>
  Task<Invitation> CreateInvitation(int validityHours);

  /// <summary>
  /// Requests the registration at the instance with the given base url.
  /// </summary>
  /// <param name="baseUrl">The base url of the partner.</param>
  /// <param name="invitationCode">The invitation code created by the admin of the partner.</param>
  /// <returns>The created <see cref="Peer"/>.</returns>
  /// <exception cref="FederationException">If the partner cannot be requested.</exception>
  Task<Peer> RequestRegistration(string baseUrl, string invitationCode);

  /// <summary>
  /// Handles a registration request sent by a partner. Requests without an open invitation are rejected before the partner is contacted.
  /// </summary>
  /// <param name="assertion">The signed registration request.</param>
  /// <exception cref="FederationException">If the request is invalid.</exception>
  Task HandleRegistrationRequest(string assertion);

  /// <summary>
  /// Approves the registration request of a partner and informs it.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <returns>The updated <see cref="Peer"/>.</returns>
  /// <exception cref="FederationException">If the partner does not wait for approval.</exception>
  Task<Peer> Approve(Guid peerId);

  /// <summary>
  /// Sends the approval to a partner waiting for the answer and links it on success.
  /// </summary>
  /// <param name="peer">The approved partner.</param>
  /// <returns>True, if the partner was informed or is linked already.</returns>
  Task<bool> CompleteApproval(Peer peer);

  /// <summary>
  /// Rejects the registration request of a partner, informs it and removes the request.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <exception cref="FederationException">If the partner does not wait for approval.</exception>
  Task Reject(Guid peerId);

  /// <summary>
  /// Handles the answer of a partner to a registration request of this instance.
  /// </summary>
  /// <param name="assertion">The signed answer.</param>
  /// <exception cref="FederationException">If the answer is invalid.</exception>
  Task HandleRegistrationResponse(string assertion);

  /// <summary>
  /// Removes the registration of a partner. Records are only removed if requested.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="deleteRecords">Removes the records of the partner on this instance and asks the partner to remove the records of this instance.</param>
  /// <param name="force">Removes the registration on this instance even if the partner cannot be informed.</param>
  /// <exception cref="FederationException">If the partner cannot be informed and force is not set.</exception>
  Task Unlink(Guid peerId, bool deleteRecords, bool force);

  /// <summary>
  /// Handles the removal of the registration requested by a partner.
  /// </summary>
  /// <param name="peer">The authenticated partner.</param>
  /// <param name="deleteOwnerRecords">Whether the records owned by the partner are removed.</param>
  Task HandleUnlink(Peer peer, bool deleteOwnerRecords);

  /// <summary>
  /// Validates an assertion sent by a known partner. Reloads the keys of the partner once if the assertion is signed with an unknown key.
  /// </summary>
  /// <param name="assertion">The assertion.</param>
  /// <param name="states">States the partner must be in.</param>
  /// <returns>The partner and the validated claims or null if the assertion is invalid.</returns>
  Task<(Peer Peer, IDictionary<string, object> Claims)?> Authenticate(string assertion, params PeerState[] states);

  /// <summary>
  /// Reloads the public keys of a partner.
  /// </summary>
  /// <param name="peer">The partner.</param>
  /// <returns>The updated <see cref="Peer"/>.</returns>
  /// <exception cref="FederationException">If the keys cannot be loaded.</exception>
  Task<Peer> RefreshKeys(Peer peer);

  /// <summary>
  /// Rotates the keys of this instance and informs all linked partners about a new key.
  /// </summary>
  Task RotateKeys();
}
