using Media.Application.Contracts.IO;
using Media.Application.Models.Federation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Media.Infrastructure.IO;

public class FederationUploadStore(FederationOptions options) : IFederationUploadStore
{
  private const string SESSION_FILE = "session.json";
  private const string DATA_FILE = "data.part";
  private const string COVER_FILE = "cover";
  private const int BUFFER_SIZE = 81920;

  public UploadSession? Get(Guid peerId, Guid transactionId)
  {
    var folder = SessionFolder(peerId, transactionId);
    var sessionFile = Path.Combine(folder, SESSION_FILE);
    if (!File.Exists(sessionFile))
    {
      return null;
    }

    var stored = JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(sessionFile), JsonSerializerOptions.Web);
    if (stored == null)
    {
      return null;
    }

    var dataFile = new FileInfo(Path.Combine(folder, DATA_FILE));
    return new UploadSession(peerId, transactionId, stored.Metadata, stored.Size, stored.Sha256, dataFile.Exists ? dataFile.Length : 0);
  }

  public async Task<UploadSession> Start(Guid peerId, Guid transactionId, SyncedRecord metadata, long size, string sha256, byte[]? cover)
  {
    var existing = Get(peerId, transactionId);
    if (existing != null && existing.Size == size && string.Equals(existing.Sha256, sha256, StringComparison.OrdinalIgnoreCase))
    {
      return existing;
    }

    var folder = SessionFolder(peerId, transactionId);
    if (Directory.Exists(folder))
    {
      Directory.Delete(folder, recursive: true);
    }

    Directory.CreateDirectory(folder);
    await File.WriteAllTextAsync(Path.Combine(folder, SESSION_FILE), JsonSerializer.Serialize(new StoredSession(metadata, size, sha256), JsonSerializerOptions.Web)).ConfigureAwait(false);
    await File.WriteAllBytesAsync(Path.Combine(folder, DATA_FILE), []).ConfigureAwait(false);
    if (cover != null)
    {
      await File.WriteAllBytesAsync(Path.Combine(folder, COVER_FILE), cover).ConfigureAwait(false);
    }

    return new UploadSession(peerId, transactionId, metadata, size, sha256, 0);
  }

  public async Task<(bool Accepted, long Offset)> Append(Guid peerId, Guid transactionId, long offset, Stream data)
  {
    var session = Get(peerId, transactionId) ?? throw new FederationException(FederationErrors.NotFound);
    FileStream target;
    try
    {
      // exclusive access, so parallel requests for the same transfer cannot interleave.
      target = new FileStream(DataPath(session), FileMode.Open, FileAccess.Write, FileShare.None);
    }
    catch (IOException)
    {
      return (false, session.Offset);
    }

    await using (target)
    {
      if (target.Length != offset)
      {
        return (false, target.Length);
      }

      target.Seek(0, SeekOrigin.End);
      var buffer = new byte[BUFFER_SIZE];
      int read;
      while ((read = await data.ReadAsync(buffer).ConfigureAwait(false)) > 0)
      {
        if (target.Length + read > session.Size)
        {
          target.SetLength(offset);
          throw new FederationException(FederationErrors.InvalidRecord, "Chunk exceeds the announced size.");
        }

        await target.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
      }

      return (true, target.Length);
    }
  }

  public async Task<bool> IsComplete(UploadSession session)
  {
    if (session.Offset != session.Size)
    {
      return false;
    }

    await using var stream = OpenData(session);
    var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream).ConfigureAwait(false));
    return string.Equals(hash, session.Sha256, StringComparison.OrdinalIgnoreCase);
  }

  public Stream OpenData(UploadSession session) => File.OpenRead(DataPath(session));

  public string DataPath(UploadSession session) => Path.Combine(SessionFolder(session.PeerId, session.TransactionId), DATA_FILE);

  public bool MoveData(UploadSession session, string targetPath)
  {
    try
    {
      File.Move(DataPath(session), targetPath, overwrite: false);
      return true;
    }
    catch (IOException) when (File.Exists(targetPath))
    {
      return false;
    }
  }

  public async Task<byte[]?> ReadCover(UploadSession session)
  {
    var path = Path.Combine(SessionFolder(session.PeerId, session.TransactionId), COVER_FILE);
    return File.Exists(path) ? await File.ReadAllBytesAsync(path).ConfigureAwait(false) : null;
  }

  public void Reset(UploadSession session)
  {
    File.WriteAllBytes(DataPath(session), []);
  }

  public void Remove(Guid peerId, Guid transactionId)
  {
    var folder = SessionFolder(peerId, transactionId);
    if (Directory.Exists(folder))
    {
      Directory.Delete(folder, recursive: true);
    }
  }

  public void RemoveAll(Guid peerId)
  {
    var folder = Path.Combine(options.UploadPath, peerId.ToString("N"));
    if (Directory.Exists(folder))
    {
      Directory.Delete(folder, recursive: true);
    }
  }

  public int Count(Guid peerId)
  {
    var folder = Path.Combine(options.UploadPath, peerId.ToString("N"));
    return Directory.Exists(folder) ? Directory.GetDirectories(folder).Length : 0;
  }

  public IList<(Guid PeerId, Guid TransactionId)> RemoveStale(DateTime before)
  {
    var removed = new List<(Guid PeerId, Guid TransactionId)>();
    if (!Directory.Exists(options.UploadPath))
    {
      return removed;
    }

    foreach (var folder in Directory.GetDirectories(options.UploadPath).SelectMany(Directory.GetDirectories))
    {
      var lastChange = Directory.GetFiles(folder).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(Directory.GetLastWriteTimeUtc(folder)).Max();
      if (lastChange < before)
      {
        Directory.Delete(folder, recursive: true);
        if (Guid.TryParse(Path.GetFileName(Path.GetDirectoryName(folder)), out var peerId) && Guid.TryParse(Path.GetFileName(folder), out var transactionId))
        {
          removed.Add((peerId, transactionId));
        }
      }
    }

    return removed;
  }

  private string SessionFolder(Guid peerId, Guid transactionId) => Path.Combine(options.UploadPath, peerId.ToString("N"), transactionId.ToString("N"));

  private sealed record StoredSession(SyncedRecord Metadata, long Size, string Sha256);
}
