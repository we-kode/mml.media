using System;

namespace Media.Application.Models.Federation;

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
