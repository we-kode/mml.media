using Media.Application.Models.Federation;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Media.Application.Contracts.IO;

/// <summary>
/// Stores incomplete transfers of records from partners, so they can be continued after an interruption.
/// </summary>
public interface IFederationUploadStore
{
  /// <summary>
  /// Returns the transfer or null if it does not exist.
  /// </summary>
  /// <param name="peerId">Id of the sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <returns><see cref="UploadSession"/> or null.</returns>
  UploadSession? Get(Guid peerId, Guid transactionId);

  /// <summary>
  /// Starts a transfer. An existing transfer with the same size and hash is continued, otherwise it is restarted.
  /// </summary>
  /// <param name="peerId">Id of the sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <param name="metadata">The metadata of the record.</param>
  /// <param name="size">The total size of the file in bytes.</param>
  /// <param name="sha256">The hex encoded SHA-256 hash of the file.</param>
  /// <param name="cover">The cover or null.</param>
  /// <returns>The <see cref="UploadSession"/> with the number of received bytes.</returns>
  Task<UploadSession> Start(Guid peerId, Guid transactionId, SyncedRecord metadata, long size, string sha256, byte[]? cover);

  /// <summary>
  /// Appends a chunk to a transfer if the offset matches the received bytes.
  /// </summary>
  /// <param name="peerId">Id of the sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  /// <param name="offset">The position of the chunk in the file.</param>
  /// <param name="data">The chunk.</param>
  /// <returns>Whether the chunk was appended and the number of received bytes.</returns>
  /// <exception cref="FederationException">If the transfer does not exist or the chunk exceeds the announced size.</exception>
  Task<(bool Accepted, long Offset)> Append(Guid peerId, Guid transactionId, long offset, Stream data);

  /// <summary>
  /// Returns whether all bytes were received and match the announced hash.
  /// </summary>
  /// <param name="session">The transfer.</param>
  /// <returns>True, if the file is complete and valid.</returns>
  Task<bool> IsComplete(UploadSession session);

  /// <summary>
  /// Opens the received file for reading.
  /// </summary>
  /// <param name="session">The transfer.</param>
  /// <returns>The file stream.</returns>
  Stream OpenData(UploadSession session);

  /// <summary>
  /// Returns the path of the received file.
  /// </summary>
  /// <param name="session">The transfer.</param>
  /// <returns>The file path.</returns>
  string DataPath(UploadSession session);

  /// <summary>
  /// Moves the received file to the target path. An existing file is never replaced.
  /// </summary>
  /// <param name="session">The transfer.</param>
  /// <param name="targetPath">The path the file is moved to.</param>
  /// <returns>False, if a file exists already at the target path.</returns>
  bool MoveData(UploadSession session, string targetPath);

  /// <summary>
  /// Returns the cover sent with the transfer or null.
  /// </summary>
  /// <param name="session">The transfer.</param>
  /// <returns>The cover or null.</returns>
  Task<byte[]?> ReadCover(UploadSession session);

  /// <summary>
  /// Discards the received bytes, so the transfer starts again.
  /// </summary>
  /// <param name="session">The transfer.</param>
  void Reset(UploadSession session);

  /// <summary>
  /// Removes a transfer.
  /// </summary>
  /// <param name="peerId">Id of the sending partner.</param>
  /// <param name="transactionId">The id of the transaction.</param>
  void Remove(Guid peerId, Guid transactionId);

  /// <summary>
  /// Removes all transfers of a partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  void RemoveAll(Guid peerId);

  /// <summary>
  /// Returns the number of open transfers of a partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <returns>The number of open transfers.</returns>
  int Count(Guid peerId);

  /// <summary>
  /// Removes transfers without progress since the given date.
  /// </summary>
  /// <param name="before">Transfers last changed before this date are removed.</param>
  void RemoveStale(DateTime before);
}
