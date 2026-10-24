using Media.Application.Models.Federation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationLinkRepository
{
  /// <summary>
  /// Returns the link of a local record to a partner or null if it is not synchronized with the partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="localRecordId">Id of the local record.</param>
  /// <returns><see cref="RecordLink"/> or null.</returns>
  RecordLink? GetByRecord(Guid peerId, Guid localRecordId);

  /// <summary>
  /// Returns the links of a local record to all partners including their instance names, oldest first.
  /// </summary>
  /// <param name="localRecordId">Id of the local record.</param>
  /// <returns>The links of the record.</returns>
  IList<RecordLink> ListByRecord(Guid localRecordId);

  /// <summary>
  /// Returns the link with the given transaction id or null if it does not exist.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="transactionId">The transaction id.</param>
  /// <returns><see cref="RecordLink"/> or null.</returns>
  RecordLink? GetByTransaction(Guid peerId, Guid transactionId);

  /// <summary>
  /// Returns the ids of the given records which are synchronized with the partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="localRecordIds">Ids of the local records.</param>
  /// <returns>Ids of the linked records.</returns>
  ISet<Guid> LinkedRecordIds(Guid peerId, IEnumerable<Guid> localRecordIds);

  /// <summary>
  /// Stores a new link.
  /// </summary>
  /// <param name="link">The link to be stored.</param>
  Task Create(RecordLink link);

  /// <summary>
  /// Sets the date the metadata were synchronized the last time to now.
  /// </summary>
  /// <param name="linkId">Id of the link.</param>
  Task Touch(Guid linkId);
}
