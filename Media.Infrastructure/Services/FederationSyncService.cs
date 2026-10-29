using Media.Application.Constants;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public partial class FederationSyncService(
  IFederationPeerRepository peerRepository,
  IFederationRuleRepository ruleRepository,
  IFederationLinkRepository linkRepository,
  IFederationJobRepository jobRepository,
  IFederationClient client,
  IFederationAuditLogger audit,
  IFederationAuditRepository auditRepository,
  IFederationNotifier notifier,
  IRecordRepository recordRepository,
  IRecordService recordService,
  ICoverLoader coverLoader,
  IFederationUploadStore uploadStore,
  IAudioInspector audioInspector,
  FederationOptions options) : IFederationSyncService
{
  private const string RECORDS_PATH = "/records";
  private const long MEGABYTE = 1024 * 1024;
  private const int MAX_RESTARTS = 1;

  public async Task<SyncResult> Synchronize(IEnumerable<Guid> recordIds, IReadOnlyCollection<Guid>? peerIds = null, bool includeExcluded = false)
  {
    var ids = recordIds.Distinct().ToList();
    var peers = LinkedPeers(peerIds);
    var ownRecords = peers.Count == 0
      ? []
      : recordRepository.GetRecordsByIds(ids).Where(r => r.OwnerInstance == Env.INSTANCE).ToList();
    var ownIds = ownRecords.Select(r => r.RecordId).ToList();

    var jobs = new List<FederationJob>();
    foreach (var peer in peers)
    {
      if (includeExcluded && ownIds.Count > 0)
      {
        await linkRepository.Include(peer.PeerId, ownIds).ConfigureAwait(false);
      }

      var rules = ruleRepository.ListFor(peer.PeerId, SyncDirection.Outgoing).Items;
      if (rules.Count == 0)
      {
        continue;
      }

      var linked = linkRepository.LinkedRecordIds(peer.PeerId, ownIds);
      var pending = jobRepository.PendingRecordIds(peer.PeerId, FederationJobType.PushRecord, ownIds);
      var excluded = includeExcluded ? new HashSet<Guid>() : linkRepository.ExcludedRecordIds(peer.PeerId, ownIds);
      jobs.AddRange(ownRecords
        .Where(r => !linked.Contains(r.RecordId) && !pending.Contains(r.RecordId) && !excluded.Contains(r.RecordId))
        .Where(r => rules.MostSpecific(r.Artist, r.Album, r.Genre, r.Groups.Select(g => g.Id)) != null)
        .Select(r => new FederationJob
        {
          PeerId = peer.PeerId,
          Type = FederationJobType.PushRecord,
          RecordId = r.RecordId,
          TransactionId = Guid.NewGuid(),
        }));
    }

    if (jobs.Count > 0)
    {
      await audit.Announce(await jobRepository.EnqueueRange(jobs).ConfigureAwait(false)).ConfigureAwait(false);
    }

    var queuedRecords = jobs.Select(j => j.RecordId).Distinct().Count();
    return new SyncResult(jobs.Count, ids.Count - queuedRecords);
  }

  public async Task<SyncResult> Revoke(IEnumerable<Guid> recordIds, IReadOnlyCollection<Guid>? peerIds = null)
  {
    var ids = recordIds.Distinct().ToList();
    var peers = LinkedPeers(peerIds);
    var ownIds = peers.Count == 0
      ? []
      : recordRepository.GetRecordsByIds(ids).Where(r => r.OwnerInstance == Env.INSTANCE).Select(r => r.RecordId).ToList();

    var jobs = new List<FederationJob>();
    foreach (var peer in ownIds.Count == 0 ? [] : peers)
    {
      await linkRepository.Exclude(peer.PeerId, ownIds).ConfigureAwait(false);
      var cancelled = await jobRepository.CancelPush(peer.PeerId, ownIds).ConfigureAwait(false);
      foreach (var (_, entryId) in cancelled)
      {
        await audit.Resolve(entryId, "The synchronization was revoked.").ConfigureAwait(false);
      }

      // a cancelled transfer may finish meanwhile, so its record is removed on the partner as well.
      var pending = jobRepository.PendingRecordIds(peer.PeerId, FederationJobType.RevokeRecord, ownIds);
      jobs.AddRange(linkRepository.LinkedRecordIds(peer.PeerId, ownIds)
        .Union(cancelled.Select(c => c.RecordId))
        .Where(id => !pending.Contains(id))
        .Select(id => new FederationJob
        {
          PeerId = peer.PeerId,
          Type = FederationJobType.RevokeRecord,
          RecordId = id,
        }));
    }

    if (jobs.Count > 0)
    {
      await audit.Announce(await jobRepository.EnqueueRange(jobs).ConfigureAwait(false)).ConfigureAwait(false);
    }

    return new SyncResult(jobs.Count, ids.Count - jobs.Select(j => j.RecordId).Distinct().Count());
  }

  private IList<Peer> LinkedPeers(IReadOnlyCollection<Guid>? peerIds)
  {
    var peers = peerRepository.ListByState(PeerState.Linked);
    if (peerIds == null)
    {
      return peers;
    }

    if (peerIds.Any(id => peers.All(p => p.PeerId != id)))
    {
      throw new FederationException(FederationErrors.InvalidPeer);
    }

    return [.. peers.Where(p => peerIds.Contains(p.PeerId))];
  }

  public async Task<FederationJobResult> Execute(FederationJob job, Peer peer)
  {
    if (peer.State != PeerState.Linked)
    {
      return FederationJobResult.Retry("Partner is not linked.");
    }

    return job.Type switch
    {
      FederationJobType.PushRecord => await Push(job, peer).ConfigureAwait(false),
      FederationJobType.UpdateRecord => await SendUpdate(job, peer).ConfigureAwait(false),
      FederationJobType.DeleteRecord => await SendDeletion(job, peer).ConfigureAwait(false),
      FederationJobType.RevokeRecord => await SendRevocation(job, peer).ConfigureAwait(false),
      _ => FederationJobResult.Failed($"Unsupported operation {job.Type}."),
    };
  }

  public async Task<ReceiveResult> StartReceive(Peer peer, Guid transactionId, SyncedRecord record, long size, string sha256, byte[]? cover)
  {
    Validate(record, cover);
    if (size <= 0 || size > options.MaxRecordSizeMb * MEGABYTE || !Sha256Pattern().IsMatch(sha256))
    {
      throw new FederationException(FederationErrors.InvalidRecord);
    }

    var existingLink = linkRepository.GetByTransaction(peer.PeerId, transactionId);
    if (existingLink != null)
    {
      return new ReceiveResult(ReceiveStatus.Created, existingLink.LocalRecordId);
    }

    if (await SkipIfIndexed(peer, transactionId, record).ConfigureAwait(false))
    {
      return new ReceiveResult(ReceiveStatus.Skipped, null);
    }

    if (uploadStore.Get(peer.PeerId, transactionId) == null && uploadStore.Count(peer.PeerId) >= options.MaxOpenTransfers)
    {
      throw new FederationException(FederationErrors.TooManyTransfers);
    }

    var session = await uploadStore.Start(peer.PeerId, transactionId, record, size, sha256, cover).ConfigureAwait(false);

    // the audit log shows the transfer as pending until it is finished.
    if (auditRepository.FindPending(AuditAction.RecordReceived, peer.Instance, transactionId) == null)
    {
      await audit.Log(AuditAction.RecordReceived, AuditStatus.Pending, peer.Instance, Env.INSTANCE, transactionId,
        message: $"Record '{record.Title}' is being received.").ConfigureAwait(false);
    }

    await ReportReceived(peer, transactionId, session.Offset, size).ConfigureAwait(false);
    return ReceiveResult.Pending(session.Offset);
  }

  public async Task<(bool Accepted, long Offset)> AppendChunk(Peer peer, Guid transactionId, long offset, Stream data)
  {
    var result = await uploadStore.Append(peer.PeerId, transactionId, offset, data).ConfigureAwait(false);
    var session = result.Accepted ? uploadStore.Get(peer.PeerId, transactionId) : null;
    if (session != null)
    {
      await ReportReceived(peer, transactionId, result.Offset, session.Size).ConfigureAwait(false);
    }

    return result;
  }

  public void AddProgress(IList<AuditEntry> entries)
  {
    var sent = jobRepository.Progress(entries.Where(e => e.Direction == SyncDirection.Outgoing).Select(e => e.EntryId));
    foreach (var entry in entries)
    {
      if (sent.TryGetValue(entry.EntryId, out var progress))
      {
        (entry.Transferred, entry.Size) = progress;
        continue;
      }

      if (entry is not { Direction: SyncDirection.Incoming, Action: AuditAction.RecordReceived, Status: AuditStatus.Pending, TransactionId: not null })
      {
        continue;
      }

      var sender = peerRepository.GetByInstance(entry.Instance);
      var session = sender == null ? null : uploadStore.Get(sender.PeerId, entry.TransactionId.Value);
      if (session != null)
      {
        entry.Transferred = session.Offset;
        entry.Size = session.Size;
      }
    }
  }

  public async Task DiscardStale(DateTime before)
  {
    foreach (var (peerId, transactionId) in uploadStore.RemoveStale(before))
    {
      var sender = peerRepository.Get(peerId);
      var entryId = sender == null ? null : auditRepository.FindPending(AuditAction.RecordReceived, sender.Instance, transactionId);
      if (sender != null && entryId.HasValue)
      {
        await audit.Log(AuditAction.RecordReceived, AuditStatus.Failed, sender.Instance, Env.INSTANCE, transactionId,
          message: "The transfer was discarded, because the partner did not continue it.", entryId: entryId).ConfigureAwait(false);
      }
    }
  }

  public async Task<ReceiveResult> CompleteReceive(Peer peer, Guid transactionId)
  {
    var existingLink = linkRepository.GetByTransaction(peer.PeerId, transactionId);
    if (existingLink != null)
    {
      return new ReceiveResult(ReceiveStatus.Created, existingLink.LocalRecordId);
    }

    var session = uploadStore.Get(peer.PeerId, transactionId) ?? throw new FederationException(FederationErrors.NotFound);
    var entryId = auditRepository.FindPending(AuditAction.RecordReceived, peer.Instance, transactionId);
    if (!await uploadStore.IsComplete(session).ConfigureAwait(false))
    {
      // a complete file with a wrong hash is corrupted, so the transfer starts again.
      if (session.Offset >= session.Size)
      {
        uploadStore.Reset(session);
        return ReceiveResult.Pending(0);
      }

      return ReceiveResult.Pending(session.Offset);
    }

    bool isMp3;
    await using (var data = uploadStore.OpenData(session))
    {
      isMp3 = audioInspector.IsMp3(data);
    }

    if (!isMp3)
    {
      uploadStore.Remove(peer.PeerId, transactionId);
      await audit.Log(AuditAction.RecordReceived, AuditStatus.Failed, peer.Instance, Env.INSTANCE, transactionId,
        message: $"Record '{session.Metadata.Title}' is no mp3 file.", entryId: entryId).ConfigureAwait(false);
      throw new FederationException(FederationErrors.InvalidRecord);
    }

    var record = session.Metadata;
    if (await SkipIfIndexed(peer, transactionId, record).ConfigureAwait(false))
    {
      return new ReceiveResult(ReceiveStatus.Skipped, null);
    }

    // the checksum of the owner is kept, so the same file uploaded on this instance is detected as duplicate.
    // an existing file is never replaced, e.g. when the same file is indexed on this instance meanwhile.
    var checksum = record.Checksum;
    if (!uploadStore.MoveData(session, Path.Combine(RECORDS_PATH, checksum)))
    {
      uploadStore.Remove(peer.PeerId, transactionId);
      await audit.Log(AuditAction.RecordReceived, AuditStatus.Skipped, peer.Instance, Env.INSTANCE, transactionId,
        message: $"Record '{record.Title}' exists already as file.", entryId: entryId).ConfigureAwait(false);
      return new ReceiveResult(ReceiveStatus.Skipped, null);
    }

    var coverFileName = await StoreCover(await uploadStore.ReadCover(session).ConfigureAwait(false)).ConfigureAwait(false);
    uploadStore.Remove(peer.PeerId, transactionId);
    var groups = ruleRepository.ListFor(peer.PeerId, SyncDirection.Incoming).Items
      .MostSpecific(record.Artist, record.Album, record.Genre)?.Groups.Select(g => g.Id).ToList() ?? [];

    await recordService.SaveMetaData(new RecordMetaData
    {
      Title = record.Title,
      Artist = record.Artist,
      Album = record.Album,
      Genre = record.Genre,
      Language = record.Language,
      TrackNumber = record.TrackNumber,
      Date = record.Date.ToUniversalTime(),
      Duration = record.Duration,
      Bitrate = record.Bitrate,
      Checksum = checksum,
      PhysicalFilePath = $"{RECORDS_PATH}/",
      OriginalFileName = record.Title,
      Cover = coverFileName,
      OwnerInstance = peer.Instance,
    }, groups).ConfigureAwait(false);

    var recordId = recordRepository.GetRecordIdByChecksum(checksum)
      ?? throw new InvalidOperationException("Received record was not stored.");
    await linkRepository.Create(new RecordLink
    {
      PeerId = peer.PeerId,
      LocalRecordId = recordId,
      RemoteRecordId = record.OwnerRecordId,
      Direction = SyncDirection.Incoming,
      TransactionId = transactionId,
    }).ConfigureAwait(false);

    await audit.Log(AuditAction.RecordReceived, AuditStatus.Success, peer.Instance, Env.INSTANCE, transactionId, [recordId],
      groups.Count == 0 ? $"Record '{record.Title}' is not assigned to any group." : null, entryId).ConfigureAwait(false);
    return new ReceiveResult(ReceiveStatus.Created, recordId);
  }

  public async Task<bool> ReceiveUpdate(Peer peer, Guid transactionId, SyncedRecord record, byte[]? cover)
  {
    Validate(record, cover, validateFile: false);
    var link = linkRepository.GetByTransaction(peer.PeerId, transactionId);
    if (link == null || link.Direction != SyncDirection.Incoming)
    {
      return false;
    }

    await recordService.UpdateFromOwner(new Record(link.LocalRecordId, record.Title, peer.Instance)
    {
      Artist = record.Artist,
      Album = record.Album,
      Genre = record.Genre,
      Language = record.Language,
    }, cover ?? []).ConfigureAwait(false);
    await linkRepository.Touch(link.LinkId).ConfigureAwait(false);
    await audit.Log(AuditAction.RecordUpdated, AuditStatus.Success, peer.Instance, Env.INSTANCE, transactionId, [link.LocalRecordId]).ConfigureAwait(false);
    return true;
  }

  public async Task<bool> ReceiveDeletion(Peer peer, Guid transactionId)
  {
    var link = linkRepository.GetByTransaction(peer.PeerId, transactionId);
    if (link == null || link.Direction != SyncDirection.Incoming)
    {
      return false;
    }

    await recordService.DeleteRecord(link.LocalRecordId, removeLocked: true).ConfigureAwait(false);
    await audit.Log(AuditAction.RecordDeleted, AuditStatus.Success, peer.Instance, Env.INSTANCE, transactionId, [link.LocalRecordId]).ConfigureAwait(false);
    return true;
  }

  private async Task<FederationJobResult> Push(FederationJob job, Peer peer)
  {
    var record = job.RecordId.HasValue ? recordRepository.TryGetRecord(job.RecordId.Value) : null;
    if (record == null || record.OwnerInstance != Env.INSTANCE || linkRepository.GetByRecord(peer.PeerId, record.RecordId) != null)
    {
      return FederationJobResult.Success;
    }

    var transactionId = job.TransactionId ?? Guid.NewGuid();
    var path = recordRepository.GetFilePath(record.RecordId);
    var size = new FileInfo(path).Length;
    string sha256;
    await using (var file = File.OpenRead(path))
    {
      sha256 = Convert.ToHexString(await SHA256.HashDataAsync(file).ConfigureAwait(false));
    }

    // the partner answers with the number of bytes it received already, so an interrupted transfer is continued.
    using var startContent = await CreateContent(record, transactionId).ConfigureAwait(false);
    startContent.Add(new StringContent(size.ToString(CultureInfo.InvariantCulture)), "size");
    startContent.Add(new StringContent(sha256), "sha256");
    var (error, received) = await SendStep(peer, HttpMethod.Post, "records", startContent).ConfigureAwait(false);

    var restarts = 0;
    var maxSteps = 2 * (size / (options.ChunkSizeMb * MEGABYTE) + 1) + 10;
    while (error == null && received!.Status == ReceiveStatus.Pending)
    {
      await jobRepository.SetProgress(job.JobId, received.Offset, size).ConfigureAwait(false);
      await notifier.Progress(new TransferProgress(job.AuditEntryId, record.RecordId, peer.Instance, SyncDirection.Outgoing, received.Offset, size)).ConfigureAwait(false);
      if (--maxSteps < 0)
      {
        return FederationJobResult.Retry("The partner does not accept the transfer.");
      }

      if (received.Offset < size)
      {
        (error, received) = await SendChunk(peer, transactionId, path, received.Offset, size).ConfigureAwait(false);
        continue;
      }

      (error, received) = await SendStep(peer, HttpMethod.Post, $"records/{transactionId}/complete").ConfigureAwait(false);
      if (error == null && received!.Status == ReceiveStatus.Pending && received.Offset == 0 && ++restarts > MAX_RESTARTS)
      {
        return FederationJobResult.Retry("The partner received a corrupted file.");
      }
    }

    if (error != null)
    {
      return error;
    }

    if (received!.Status == ReceiveStatus.Created && received.RecordId.HasValue)
    {
      await linkRepository.Create(new RecordLink
      {
        PeerId = peer.PeerId,
        LocalRecordId = record.RecordId,
        RemoteRecordId = received.RecordId.Value,
        Direction = SyncDirection.Outgoing,
        TransactionId = transactionId,
      }).ConfigureAwait(false);
      await audit.Log(AuditAction.RecordPushed, AuditStatus.Success, Env.INSTANCE, peer.Instance, transactionId, [record.RecordId], entryId: job.AuditEntryId).ConfigureAwait(false);
    }
    else
    {
      await audit.Log(AuditAction.RecordPushed, AuditStatus.Skipped, Env.INSTANCE, peer.Instance, transactionId, [record.RecordId],
        $"Record '{record.Title}' exists already on the partner.", job.AuditEntryId).ConfigureAwait(false);
    }

    return FederationJobResult.Success;
  }

  private async Task<(FederationJobResult? Error, ReceiveResult? Received)> SendChunk(Peer peer, Guid transactionId, string path, long offset, long size)
  {
    var length = (int)Math.Min(options.ChunkSizeMb * MEGABYTE, size - offset);
    var buffer = ArrayPool<byte>.Shared.Rent(length);
    try
    {
      await using (var file = File.OpenRead(path))
      {
        file.Seek(offset, SeekOrigin.Begin);
        await file.ReadExactlyAsync(buffer.AsMemory(0, length)).ConfigureAwait(false);
      }

      using var content = new ByteArrayContent(buffer, 0, length);
      content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
      return await SendStep(peer, HttpMethod.Post, $"records/{transactionId}/content?offset={offset}", content).ConfigureAwait(false);
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  private async Task<(FederationJobResult? Error, ReceiveResult? Received)> SendStep(Peer peer, HttpMethod method, string path, HttpContent? content = null)
  {
    using var response = await client.Send(peer.Endpoint, peer.Instance, method, path, content).ConfigureAwait(false);

    // a conflict contains the offset the partner expects next.
    if (response.StatusCode != HttpStatusCode.Conflict)
    {
      var result = FederationJobResult.FromResponse(response);
      if (result != FederationJobResult.Success)
      {
        return (result, null);
      }
    }

    var received = await response.Content.ReadFromJsonAsync<ReceiveResult>().ConfigureAwait(false);
    return received == null ? (FederationJobResult.Retry("The partner returned an invalid answer."), null) : (null, received);
  }

  private async Task<FederationJobResult> SendUpdate(FederationJob job, Peer peer)
  {
    var record = job.RecordId.HasValue ? recordRepository.TryGetRecord(job.RecordId.Value) : null;
    var link = record == null ? null : linkRepository.GetByRecord(peer.PeerId, record.RecordId);
    if (record == null || link == null)
    {
      return FederationJobResult.Success;
    }

    using var content = await CreateContent(record, link.TransactionId).ConfigureAwait(false);
    using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, $"records/{link.TransactionId}", content).ConfigureAwait(false);

    // the partner may have removed the record, which the owner is not informed about.
    var result = FederationJobResult.FromResponse(response, notFoundIsSuccess: true);
    if (response.IsSuccessStatusCode)
    {
      await linkRepository.Touch(link.LinkId).ConfigureAwait(false);
      await audit.Log(AuditAction.RecordUpdated, AuditStatus.Success, Env.INSTANCE, peer.Instance, link.TransactionId, [record.RecordId], entryId: job.AuditEntryId).ConfigureAwait(false);
    }

    return result;
  }

  private async Task<FederationJobResult> SendDeletion(FederationJob job, Peer peer)
  {
    if (!job.TransactionId.HasValue)
    {
      return FederationJobResult.Success;
    }

    using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Delete, $"records/{job.TransactionId}").ConfigureAwait(false);
    var result = FederationJobResult.FromResponse(response, notFoundIsSuccess: true);
    if (result == FederationJobResult.Success)
    {
      await audit.Log(AuditAction.RecordDeleted, AuditStatus.Success, Env.INSTANCE, peer.Instance, job.TransactionId,
        job.RecordId.HasValue ? [job.RecordId.Value] : null, entryId: job.AuditEntryId).ConfigureAwait(false);
    }

    return result;
  }

  private async Task<FederationJobResult> SendRevocation(FederationJob job, Peer peer)
  {
    var link = job.RecordId.HasValue ? linkRepository.GetByRecord(peer.PeerId, job.RecordId.Value) : null;
    if (link == null || link.Direction != SyncDirection.Outgoing)
    {
      return FederationJobResult.Success;
    }

    using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Delete, $"records/{link.TransactionId}").ConfigureAwait(false);
    var result = FederationJobResult.FromResponse(response, notFoundIsSuccess: true);
    if (result == FederationJobResult.Success)
    {
      await linkRepository.Delete(peer.PeerId, link.LocalRecordId).ConfigureAwait(false);
      await audit.Log(AuditAction.RecordRevoked, AuditStatus.Success, Env.INSTANCE, peer.Instance, link.TransactionId,
        [link.LocalRecordId], entryId: job.AuditEntryId).ConfigureAwait(false);
    }

    return result;
  }

  private async Task<MultipartFormDataContent> CreateContent(Record record, Guid transactionId)
  {
    var metadata = new SyncedRecord
    {
      OwnerRecordId = record.RecordId,
      Checksum = record.Checksum ?? string.Empty,
      Title = record.Title,
      Artist = record.Artist,
      Album = record.Album,
      Genre = record.Genre,
      Language = record.Language,
      TrackNumber = record.TrackNumber,
      Date = record.Date,
      Duration = TimeSpan.FromMilliseconds(record.Duration),
      Bitrate = record.Bitrate,
    };

    var content = new MultipartFormDataContent
    {
      { new StringContent(transactionId.ToString()), "transactionId" },
      { new StringContent(JsonSerializer.Serialize(metadata, JsonSerializerOptions.Web), Encoding.UTF8, "application/json"), "metadata" },
    };

    var cover = string.IsNullOrEmpty(record.Cover) ? null : await coverLoader.Load(record.Cover).ConfigureAwait(false);
    if (cover != null)
    {
      content.Add(new ByteArrayContent(cover), "cover", record.Cover!);
    }

    return content;
  }

  private static void Validate(SyncedRecord record, byte[]? cover, bool validateFile = true)
  {
    if ((validateFile && !ChecksumPattern().IsMatch(record.Checksum))
      || string.IsNullOrWhiteSpace(record.Title)
      || record.Duration < TimeSpan.Zero
      || record.Bitrate < 0
      || record.TrackNumber < 0
      || (cover != null && !IsImage(cover)))
    {
      throw new FederationException(FederationErrors.InvalidRecord);
    }
  }

  private static bool IsImage(byte[] data)
  {
    if (data.Length == 0)
    {
      return false;
    }

    return data.AsSpan().StartsWith((byte[])[0xFF, 0xD8, 0xFF])
      || data.AsSpan().StartsWith((byte[])[0x89, 0x50, 0x4E, 0x47])
      || data.AsSpan().StartsWith("GIF8"u8)
      || (data.Length > 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8));
  }

  private async Task<bool> SkipIfIndexed(Peer peer, Guid transactionId, SyncedRecord record)
  {
    if (!recordRepository.IsIndexed(record.Checksum))
    {
      return false;
    }

    uploadStore.Remove(peer.PeerId, transactionId);
    await audit.Log(AuditAction.RecordReceived, AuditStatus.Skipped, peer.Instance, Env.INSTANCE, transactionId,
      message: $"Record '{record.Title}' exists already.", entryId: auditRepository.FindPending(AuditAction.RecordReceived, peer.Instance, transactionId)).ConfigureAwait(false);
    return true;
  }

  private async Task ReportReceived(Peer peer, Guid transactionId, long transferred, long size)
  {
    var entryId = auditRepository.FindPending(AuditAction.RecordReceived, peer.Instance, transactionId);
    await notifier.Progress(new TransferProgress(entryId, null, peer.Instance, SyncDirection.Incoming, transferred, size)).ConfigureAwait(false);
  }

  private async Task<string?> StoreCover(byte[]? cover)
  {
    if (cover == null)
    {
      return null;
    }

    var fileName = cover.ToFileName();
    await coverLoader.Save(cover, fileName).ConfigureAwait(false);
    return fileName;
  }

  [GeneratedRegex("^[0-9a-fA-F]{40}$")]
  private static partial Regex ChecksumPattern();

  [GeneratedRegex("^[0-9a-fA-F]{64}$")]
  private static partial Regex Sha256Pattern();
}
