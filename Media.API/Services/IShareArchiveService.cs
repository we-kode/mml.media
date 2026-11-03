using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.Services;

/// <summary>
/// Provides the files of shared records for download.
/// </summary>
public interface IShareArchiveService
{
  /// <summary>
  /// Opens the file of a record.
  /// </summary>
  /// <param name="recordId">Id of the record.</param>
  /// <returns>The opened file or null, if the record or its file does not exist.</returns>
  RecordStream? Open(Guid recordId);

  /// <summary>
  /// Writes the files of records as zip archive. Records without file are skipped.
  /// </summary>
  /// <param name="recordIds">Ids of the records.</param>
  /// <param name="target">Stream the archive is written to.</param>
  /// <param name="cancellationToken">Cancels the writing.</param>
  Task WriteZip(IEnumerable<Guid> recordIds, Stream target, CancellationToken cancellationToken);
}
