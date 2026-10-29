using Media.Application.Types;
using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// The progress of a transfer of a record between this instance and a partner.
/// </summary>
/// <param name="EntryId">The audit log entry of the transfer if available.</param>
/// <param name="RecordId">The id of the local record if available.</param>
/// <param name="Instance">The instance name of the partner.</param>
/// <param name="Direction">Whether the record is sent or received.</param>
/// <param name="Transferred">The number of transferred bytes.</param>
/// <param name="Size">The size of the file in bytes.</param>
public record TransferProgress(Guid? EntryId, Guid? RecordId, string Instance, SyncDirection Direction, long Transferred, long Size);
