using Media.API.Share;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.Services;

public class ShareArchiveService(IRecordRepository recordRepository) : IShareArchiveService
{
  public RecordStream? Open(Guid recordId)
  {
    try
    {
      return recordRepository.StreamRecord(recordId);
    }
    catch (Exception e) when (e is IOException or InvalidOperationException)
    {
      return null;
    }
  }

  public async Task WriteZip(IEnumerable<Guid> recordIds, Stream target, CancellationToken cancellationToken)
  {
    var names = new HashSet<string>();
    using var archive = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: true);
    foreach (var record in recordRepository.GetRecordsByIds(recordIds).OrderBy(r => r.Title))
    {
      var stream = Open(record.RecordId);
      if (stream == null)
      {
        continue;
      }

      using (stream.Stream)
      {
        var entry = archive.CreateEntry(ShareFileNames.Create(stream, names), CompressionLevel.NoCompression);
        using var entryStream = entry.Open();
        await stream.Stream.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}
