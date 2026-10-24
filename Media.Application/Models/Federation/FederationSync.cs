using System;
using System.Net;
using System.Net.Http;

namespace Media.Application.Models.Federation;

/// <summary>
/// Metadata of a record sent to a partner.
/// </summary>
public class SyncedRecord
{
  /// <summary>
  /// Id of the record on the owning instance.
  /// </summary>
  public Guid OwnerRecordId { get; set; }

  /// <summary>
  /// SHA-1 checksum of the original file, used as file name.
  /// </summary>
  public string Checksum { get; set; } = string.Empty;

  /// <summary>
  /// Title of the record.
  /// </summary>
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Artist of the record.
  /// </summary>
  public string? Artist { get; set; }

  /// <summary>
  /// Album of the record.
  /// </summary>
  public string? Album { get; set; }

  /// <summary>
  /// Genre of the record.
  /// </summary>
  public string? Genre { get; set; }

  /// <summary>
  /// Language of the record.
  /// </summary>
  public string? Language { get; set; }

  /// <summary>
  /// Track number of the record.
  /// </summary>
  public int TrackNumber { get; set; }

  /// <summary>
  /// Date of the record.
  /// </summary>
  public DateTime Date { get; set; }

  /// <summary>
  /// Duration of the record.
  /// </summary>
  public TimeSpan Duration { get; set; }

  /// <summary>
  /// Bitrate of the record in kbit/s.
  /// </summary>
  public int Bitrate { get; set; }
}

/// <summary>
/// Results of receiving a record.
/// </summary>
public enum ReceiveStatus
{
  /// <summary>
  /// The record was stored.
  /// </summary>
  Created = 1,

  /// <summary>
  /// The record exists already and was not stored again.
  /// </summary>
  Skipped = 2,

  /// <summary>
  /// The transfer is not finished. The sender continues at the returned offset.
  /// </summary>
  Pending = 3,
}

/// <summary>
/// Answer of a partner to a sent record.
/// </summary>
/// <param name="Status">Whether the record was stored.</param>
/// <param name="RecordId">The id of the record on the partner if it was stored.</param>
/// <param name="Offset">The number of bytes the partner received if the transfer is pending.</param>
public record ReceiveResult(ReceiveStatus Status, Guid? RecordId, long Offset = 0)
{
  /// <summary>
  /// A pending transfer continuing at the given offset.
  /// </summary>
  /// <param name="offset">The number of received bytes.</param>
  /// <returns><see cref="ReceiveResult"/></returns>
  public static ReceiveResult Pending(long offset) => new(ReceiveStatus.Pending, null, offset);
}

/// <summary>
/// An incomplete transfer of a record from a partner.
/// </summary>
/// <param name="PeerId">Id of the sending partner.</param>
/// <param name="TransactionId">The id of the transaction.</param>
/// <param name="Metadata">The metadata of the record.</param>
/// <param name="Size">The total size of the file in bytes.</param>
/// <param name="Sha256">The hex encoded SHA-256 hash of the file.</param>
/// <param name="Offset">The number of received bytes.</param>
public record UploadSession(Guid PeerId, Guid TransactionId, SyncedRecord Metadata, long Size, string Sha256, long Offset);

/// <summary>
/// Result of a manual synchronization.
/// </summary>
/// <param name="Queued">Number of queued transfers to partners.</param>
/// <param name="Skipped">Number of records not queued for any partner, because they are not owned, synchronized already or match no rule.</param>
public record SyncResult(int Queued, int Skipped);

/// <summary>
/// Result of an executed operation against a partner.
/// </summary>
/// <param name="Done">Whether the operation is finished and removed from the queue.</param>
/// <param name="Error">The error, if the operation failed.</param>
public record FederationJobResult(bool Done, string? Error)
{
  /// <summary>
  /// The operation succeeded.
  /// </summary>
  public static FederationJobResult Success { get; } = new(true, null);

  /// <summary>
  /// The operation failed temporarily and is retried.
  /// </summary>
  /// <param name="error">The error.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult Retry(string error) => new(false, error);

  /// <summary>
  /// The operation failed permanently.
  /// </summary>
  /// <param name="error">The error.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult Failed(string error) => new(true, error);

  /// <summary>
  /// Evaluates the response of a partner. Server errors, timeouts and throttling are retried.
  /// </summary>
  /// <param name="response">The response.</param>
  /// <param name="notFoundIsSuccess">Treats a missing resource as success.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult FromResponse(HttpResponseMessage response, bool notFoundIsSuccess = false)
  {
    if (response.IsSuccessStatusCode || (notFoundIsSuccess && response.StatusCode == HttpStatusCode.NotFound))
    {
      return Success;
    }

    var status = (int)response.StatusCode;
    if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
    {
      return Failed("The data is too large for the upload limit of the partner (413).");
    }

    var transient = status >= 500 || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
    return transient ? Retry($"Partner returned {status}.") : Failed($"Partner returned {status}.");
  }
}
