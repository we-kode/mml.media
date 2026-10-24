using Media.Application.Models.Federation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Synchronizes records with linked partner instances.
/// </summary>
public interface IFederationSyncService
{
  /// <summary>
  /// Queues the given records for every linked partner with a matching outgoing rule.
  /// Records owned by other instances and records already sent to a partner are skipped.
  /// </summary>
  /// <param name="recordIds">Ids of the local records.</param>
  /// <returns><see cref="SyncResult"/></returns>
  Task<SyncResult> Synchronize(IEnumerable<Guid> recordIds);

  /// <summary>
  /// Executes a queued record operation against a partner.
  /// </summary>
  /// <param name="job">The operation.</param>
  /// <param name="peer">The partner.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  Task<FederationJobResult> Execute(FederationJob job, Peer peer);

  /// <summary>
  /// Starts or continues the transfer of a record sent by its owner.
  /// A record received already returns its id, a record whose checksum exists already is skipped.
  /// </summary>
  /// <param name="peer">The sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <param name="record">The metadata of the record.</param>
  /// <param name="size">The size of the file in bytes.</param>
  /// <param name="sha256">The hex encoded SHA-256 hash of the file.</param>
  /// <param name="cover">The cover or null.</param>
  /// <returns><see cref="ReceiveResult"/> with the offset to continue at if the transfer is pending.</returns>
  /// <exception cref="FederationException">If the metadata, size or cover are invalid or the partner has too many open transfers.</exception>
  Task<ReceiveResult> StartReceive(Peer peer, Guid transactionId, SyncedRecord record, long size, string sha256, byte[]? cover);

  /// <summary>
  /// Appends a chunk to a pending transfer if the offset matches the received bytes.
  /// </summary>
  /// <param name="peer">The sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <param name="offset">The position of the chunk in the file.</param>
  /// <param name="data">The chunk.</param>
  /// <returns>Whether the chunk was appended and the number of received bytes.</returns>
  /// <exception cref="FederationException">If the transfer does not exist or the chunk exceeds the announced size.</exception>
  Task<(bool Accepted, long Offset)> AppendChunk(Peer peer, Guid transactionId, long offset, Stream data);

  /// <summary>
  /// Finishes a transfer. The file must be complete, match the announced hash and be an mp3 file.
  /// The record is assigned to the groups of the most specific incoming rule.
  /// A corrupted file is discarded and the transfer restarts at offset 0.
  /// </summary>
  /// <param name="peer">The sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <returns><see cref="ReceiveResult"/>, pending with the offset to continue at if the file is not complete.</returns>
  /// <exception cref="FederationException">If the transfer does not exist or the file is no mp3 file.</exception>
  Task<ReceiveResult> CompleteReceive(Peer peer, Guid transactionId);

  /// <summary>
  /// Updates the metadata of a record with the changes sent by its owner.
  /// </summary>
  /// <param name="peer">The sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <param name="record">The new metadata.</param>
  /// <param name="cover">The cover or null.</param>
  /// <returns>False, if the record does not exist on this instance.</returns>
  /// <exception cref="FederationException">If the metadata or the cover are invalid.</exception>
  Task<bool> ReceiveUpdate(Peer peer, Guid transactionId, SyncedRecord record, byte[]? cover);

  /// <summary>
  /// Removes a record on request of its owner, even if it is locked.
  /// </summary>
  /// <param name="peer">The sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <returns>False, if the record does not exist on this instance.</returns>
  Task<bool> ReceiveDeletion(Peer peer, Guid transactionId);
}
