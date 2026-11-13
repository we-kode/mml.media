using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationPeerRepository
{
  /// <summary>
  /// Returns a list of partner instances.
  /// </summary>
  /// <param name="filter">Partners will be filtered by instance name or url.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="Peers"/></returns>
  Peers List(string? filter, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Returns all partners in one of the given states.
  /// </summary>
  /// <param name="states">States of the partners to load.</param>
  /// <returns>List of <see cref="Peer"/>.</returns>
  IList<Peer> ListByState(params PeerState[] states);

  /// <summary>
  /// Returns one partner or null if it does not exist.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <returns><see cref="Peer"/> or null.</returns>
  Peer? Get(Guid peerId);

  /// <summary>
  /// Returns the partner with the given instance name or null if it does not exist. The name is compared case-insensitive.
  /// </summary>
  /// <param name="instance">Instance name of the partner.</param>
  /// <returns><see cref="Peer"/> or null.</returns>
  Peer? GetByInstance(string instance);

  /// <summary>
  /// Creates a new partner.
  /// </summary>
  /// <param name="peer">Partner to be created.</param>
  /// <returns>The created <see cref="Peer"/>.</returns>
  Task<Peer> Create(Peer peer);

  /// <summary>
  /// Updates state, request id and public keys of one partner.
  /// </summary>
  /// <param name="peer">Partner to be updated.</param>
  Task Update(Peer peer);

  /// <summary>
  /// Removes one partner including its links, pending operations and the rules restricted to this partner only.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  Task Delete(Guid peerId);
}
