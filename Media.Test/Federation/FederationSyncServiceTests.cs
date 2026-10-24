using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.IO;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using Record = Media.Application.Models.Record;

namespace Media.Test.Federation;

public class FederationSyncServiceTests
{
  private readonly Mock<IFederationPeerRepository> peers = new();
  private readonly Mock<IFederationRuleRepository> rules = new();
  private readonly Mock<IFederationLinkRepository> links = new();
  private readonly Mock<IFederationJobRepository> jobs = new();
  private readonly Mock<IFederationClient> client = new();
  private readonly Mock<IFederationAuditLogger> audit = new();
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IRecordService> recordService = new();
  private readonly Mock<ICoverLoader> covers = new();
  private readonly Mock<IAudioInspector> audioInspector = new();
  private readonly FederationOptions options = new() { UploadPath = TestEnvironment.CreateTempFolder(), ChunkSizeMb = 1 };
  private readonly FederationUploadStore uploadStore;
  private readonly List<Record> knownRecords = [];
  private readonly Peer peer = new()
  {
    PeerId = Guid.NewGuid(),
    Instance = "beta",
    Endpoint = $"https://beta.example{FederationOptions.EndpointPath}",
    State = PeerState.Linked,
  };

  public FederationSyncServiceTests()
  {
    uploadStore = new FederationUploadStore(options);
    peers.Setup(p => p.ListByState(PeerState.Linked)).Returns([peer]);
    links.Setup(l => l.LinkedRecordIds(peer.PeerId, It.IsAny<IEnumerable<Guid>>())).Returns(new HashSet<Guid>());
    rules.Setup(r => r.ListFor(peer.PeerId, It.IsAny<SyncDirection?>())).Returns(new Rules());
    jobs.Setup(j => j.PendingRecordIds(peer.PeerId, FederationJobType.PushRecord, It.IsAny<IEnumerable<Guid>>())).Returns(new HashSet<Guid>());
    records.Setup(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>()))
      .Returns((IEnumerable<Guid> ids) => [.. knownRecords.Where(r => ids.Contains(r.RecordId))]);
  }

  private void VerifyQueued(params Guid[] recordIds)
  {
    jobs.Verify(j => j.EnqueueRange(It.Is<IEnumerable<FederationJob>>(queued =>
      queued.Count() == recordIds.Length
      && queued.All(job => job.PeerId == peer.PeerId && job.Type == FederationJobType.PushRecord && job.TransactionId.HasValue && recordIds.Contains(job.RecordId!.Value)))), Times.Once);
  }

  private FederationSyncService CreateService() => new(
    peers.Object, rules.Object, links.Object, jobs.Object, client.Object, audit.Object, records.Object, recordService.Object, covers.Object,
    uploadStore, audioInspector.Object, options);

  private Record AddRecord(string owner = TestEnvironment.Instance, string artist = "Artist A")
  {
    var record = new Record(Guid.NewGuid(), "Title", owner) { Artist = artist, Album = "Album", Genre = "Rock", Checksum = new string('a', 40) };
    records.Setup(r => r.TryGetRecord(record.RecordId)).Returns(record);
    knownRecords.Add(record);
    return record;
  }

  private void OutgoingRules(params Rule[] outgoing)
  {
    rules.Setup(r => r.ListFor(peer.PeerId, SyncDirection.Outgoing)).Returns(new Rules { TotalCount = outgoing.Length, Items = outgoing });
  }

  private static SyncedRecord Metadata(string checksum) => new() { OwnerRecordId = Guid.NewGuid(), Checksum = checksum, Title = "Title", Artist = "Artist A" };

  [Fact]
  public async Task Synchronize_QueuesOneTransferPerPartnerForOverlappingRules()
  {
    var record = AddRecord();
    OutgoingRules(new Rule { Artists = [new Artist { ArtistId = Guid.NewGuid(), Name = "Artist A" }] }, new Rule { Genres = [new Genre { GenreId = Guid.NewGuid(), Name = "Rock" }] }, new Rule());

    var result = await CreateService().Synchronize([record.RecordId, record.RecordId]);

    Assert.Equal(new SyncResult(1, 0), result);
    VerifyQueued(record.RecordId);
  }

  [Fact]
  public async Task Synchronize_SkipsRecordsWithoutRuleForeignOwnerOrExistingLink()
  {
    OutgoingRules(new Rule { Artists = [new Artist { ArtistId = Guid.NewGuid(), Name = "Artist A" }] });
    var noRule = AddRecord(artist: "Artist B");
    var foreign = AddRecord(owner: "gamma");
    var linked = AddRecord();
    var pending = AddRecord();
    links.Setup(l => l.LinkedRecordIds(peer.PeerId, It.IsAny<IEnumerable<Guid>>())).Returns(new HashSet<Guid> { linked.RecordId });
    jobs.Setup(j => j.PendingRecordIds(peer.PeerId, FederationJobType.PushRecord, It.IsAny<IEnumerable<Guid>>())).Returns(new HashSet<Guid> { pending.RecordId });

    var result = await CreateService().Synchronize([noRule.RecordId, foreign.RecordId, linked.RecordId, pending.RecordId, Guid.NewGuid()]);

    Assert.Equal(new SyncResult(0, 5), result);
    jobs.Verify(j => j.EnqueueRange(It.IsAny<IEnumerable<FederationJob>>()), Times.Never);
  }

  [Fact]
  public async Task Synchronize_SendsOnlyRecordsOfTheGroupsOfOutgoingRules()
  {
    var kids = new Group(Guid.NewGuid(), "Kids", false);
    OutgoingRules(new Rule { Direction = SyncDirection.Outgoing, Groups = [kids] });
    var inGroup = AddRecord();
    inGroup.Groups = [kids];
    var otherGroup = AddRecord();
    otherGroup.Groups = [new Group(Guid.NewGuid(), "Adults", false)];

    var result = await CreateService().Synchronize([inGroup.RecordId, otherGroup.RecordId]);

    Assert.Equal(new SyncResult(1, 1), result);
    VerifyQueued(inGroup.RecordId);
  }

  [Fact]
  public async Task Synchronize_LoadsAndQueuesManyRecordsAtOnce()
  {
    OutgoingRules(new Rule { Genres = [new Genre { GenreId = Guid.NewGuid(), Name = "Rock" }] });
    var ids = Enumerable.Range(0, 1000).Select(_ => AddRecord().RecordId).ToArray();

    var result = await CreateService().Synchronize(ids);

    Assert.Equal(new SyncResult(1000, 0), result);
    records.Verify(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Once);
    records.Verify(r => r.TryGetRecord(It.IsAny<Guid>()), Times.Never);
    links.Verify(l => l.LinkedRecordIds(peer.PeerId, It.IsAny<IEnumerable<Guid>>()), Times.Once);
    jobs.Verify(j => j.PendingRecordIds(peer.PeerId, FederationJobType.PushRecord, It.IsAny<IEnumerable<Guid>>()), Times.Once);
    VerifyQueued(ids);
  }

  [Fact]
  public async Task Synchronize_DoesNothingWithoutRules()
  {
    var record = AddRecord();

    var result = await CreateService().Synchronize([record.RecordId]);

    Assert.Equal(new SyncResult(0, 1), result);
  }

  private static readonly string Sha256 = new('a', 64);

  private Task<ReceiveResult> Start(SyncedRecord record, Guid? transactionId = null, long size = 100, string? sha256 = null, byte[]? cover = null)
    => CreateService().StartReceive(peer, transactionId ?? Guid.NewGuid(), record, size, sha256 ?? Sha256, cover);

  [Theory]
  [InlineData("../../etc/passwd")]
  [InlineData("not-a-checksum")]
  [InlineData("")]
  public async Task StartReceive_RejectsInvalidChecksum(string checksum)
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => Start(Metadata(checksum)));

    Assert.Equal(FederationErrors.InvalidRecord, exception.Code);
  }

  [Theory]
  [InlineData(0L, null)]
  [InlineData(3L * 1024 * 1024 * 1024, null)]
  [InlineData(100L, "no-hash")]
  public async Task StartReceive_RejectsInvalidSizeOrHash(long size, string? sha256)
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => Start(Metadata(new string('a', 40)), size: size, sha256: sha256));

    Assert.Equal(FederationErrors.InvalidRecord, exception.Code);
  }

  [Fact]
  public async Task StartReceive_RejectsCoverWhichIsNoImage()
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => Start(Metadata(new string('a', 40)), cover: "<html>"u8.ToArray()));

    Assert.Equal(FederationErrors.InvalidRecord, exception.Code);
  }

  [Fact]
  public async Task StartReceive_SkipsExistingChecksum()
  {
    var checksum = new string('b', 40);
    records.Setup(r => r.IsIndexed(checksum)).Returns(true);

    var result = await Start(Metadata(checksum));

    Assert.Equal(ReceiveStatus.Skipped, result.Status);
    Assert.Equal(0, uploadStore.Count(peer.PeerId));
  }

  [Fact]
  public async Task StartReceive_ReturnsStoredRecordForRepeatedTransaction()
  {
    var transactionId = Guid.NewGuid();
    var localId = Guid.NewGuid();
    links.Setup(l => l.GetByTransaction(peer.PeerId, transactionId)).Returns(new RecordLink { LocalRecordId = localId, Direction = SyncDirection.Incoming });

    var result = await Start(Metadata(new string('c', 40)), transactionId);

    Assert.Equal(new ReceiveResult(ReceiveStatus.Created, localId), result);
  }

  [Fact]
  public async Task StartReceive_ContinuesAtReceivedOffset()
  {
    var transactionId = Guid.NewGuid();
    Assert.Equal(ReceiveResult.Pending(0), await Start(Metadata(new string('d', 40)), transactionId));
    await CreateService().AppendChunk(peer, transactionId, 0, new MemoryStream(new byte[40]));

    Assert.Equal(ReceiveResult.Pending(40), await Start(Metadata(new string('d', 40)), transactionId));
  }

  [Fact]
  public async Task StartReceive_LimitsOpenTransfersPerPartner()
  {
    options.MaxOpenTransfers = 2;
    for (var i = 0; i < 2; i++)
    {
      await Start(Metadata(new string('e', 40)));
    }

    var exception = await Assert.ThrowsAsync<FederationException>(() => Start(Metadata(new string('e', 40))));

    Assert.Equal(FederationErrors.TooManyTransfers, exception.Code);
  }

  [Fact]
  public async Task StartReceive_AcceptsLongTextsAndLargeCovers()
  {
    var record = Metadata(new string('f', 40));
    record.Title = new string('t', 2000);
    var cover = new byte[8 * 1024 * 1024];
    ((byte[])[0x89, 0x50, 0x4E, 0x47]).CopyTo(cover, 0);

    Assert.Equal(ReceiveResult.Pending(0), await Start(record, cover: cover));
  }

  [Fact]
  public async Task CompleteReceive_ReturnsOffsetOfIncompleteTransfer()
  {
    var transactionId = Guid.NewGuid();
    await Start(Metadata(new string('f', 40)), transactionId);
    await CreateService().AppendChunk(peer, transactionId, 0, new MemoryStream(new byte[30]));

    Assert.Equal(ReceiveResult.Pending(30), await CreateService().CompleteReceive(peer, transactionId));
  }

  [Fact]
  public async Task CompleteReceive_RestartsCorruptedTransfer()
  {
    var transactionId = Guid.NewGuid();
    await Start(Metadata(new string('f', 40)), transactionId);
    await CreateService().AppendChunk(peer, transactionId, 0, new MemoryStream(new byte[100]));

    Assert.Equal(ReceiveResult.Pending(0), await CreateService().CompleteReceive(peer, transactionId));
    Assert.Equal(0, uploadStore.Get(peer.PeerId, transactionId)!.Offset);
  }

  [Fact]
  public async Task CompleteReceive_RejectsFileWhichIsNoMp3()
  {
    var transactionId = Guid.NewGuid();
    var data = RandomNumberGenerator.GetBytes(100);
    await Start(Metadata(new string('f', 40)), transactionId, sha256: Convert.ToHexString(SHA256.HashData(data)));
    await CreateService().AppendChunk(peer, transactionId, 0, new MemoryStream(data));

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().CompleteReceive(peer, transactionId));

    Assert.Equal(FederationErrors.InvalidRecord, exception.Code);
    Assert.Null(uploadStore.Get(peer.PeerId, transactionId));
    recordService.Verify(r => r.SaveMetaData(It.IsAny<RecordMetaData>(), It.IsAny<List<Guid>>()), Times.Never);
  }

  private (Record Record, byte[] Data) AddRecordWithFile(int size)
  {
    var record = AddRecord();
    var data = RandomNumberGenerator.GetBytes(size);
    var path = Path.Combine(TestEnvironment.CreateTempFolder(), "record.mp3");
    File.WriteAllBytes(path, data);
    records.Setup(r => r.GetFilePath(record.RecordId)).Returns(path);
    return (record, data);
  }

  // simulates a partner which received the given bytes already and fails or conflicts once at the given offsets.
  private List<long> SimulatePartner(byte[] data, long alreadyReceived, long? failingOffset = null, long? conflictOffset = null)
  {
    var offsets = new List<long>();
    var received = alreadyReceived;
    client
      .Setup(c => c.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent?>(), null))
      .ReturnsAsync((string _, string _, HttpMethod _, string path, HttpContent? content, IDictionary<string, object>? _) =>
      {
        if (path == "records")
        {
          return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(ReceiveResult.Pending(received)) };
        }

        if (path.EndsWith("/complete"))
        {
          return received == data.Length
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ReceiveResult(ReceiveStatus.Created, Guid.Empty)) }
            : new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(ReceiveResult.Pending(received)) };
        }

        var offset = long.Parse(path[(path.IndexOf("offset=") + 7)..]);
        offsets.Add(offset);
        if (offset == failingOffset)
        {
          failingOffset = null;
          return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        if (offset == conflictOffset)
        {
          conflictOffset = null;
          received = offset + 10;
          return new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(ReceiveResult.Pending(received)) };
        }

        received = offset + content!.Headers.ContentLength!.Value;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(ReceiveResult.Pending(received)) };
      });
    return offsets;
  }

  private Task<FederationJobResult> Push(Record record, Guid transactionId)
    => CreateService().Execute(new FederationJob { Type = FederationJobType.PushRecord, RecordId = record.RecordId, TransactionId = transactionId, PeerId = peer.PeerId }, peer);

  [Fact]
  public async Task Execute_SendsRecordInChunks()
  {
    var (record, data) = AddRecordWithFile(2_500_000);
    var offsets = SimulatePartner(data, 0);
    var transactionId = Guid.NewGuid();

    var result = await Push(record, transactionId);

    Assert.Equal(FederationJobResult.Success, result);
    Assert.Equal([0L, 1_048_576L, 2_097_152L], offsets);
    links.Verify(l => l.Create(It.Is<RecordLink>(link => link.TransactionId == transactionId && link.Direction == SyncDirection.Outgoing)), Times.Once);
  }

  [Fact]
  public async Task Execute_ContinuesInterruptedTransfer()
  {
    var (record, data) = AddRecordWithFile(2_500_000);
    var offsets = SimulatePartner(data, alreadyReceived: 0, failingOffset: 1_048_576);
    var transactionId = Guid.NewGuid();

    var interrupted = await Push(record, transactionId);
    Assert.False(interrupted.Done);
    links.Verify(l => l.Create(It.IsAny<RecordLink>()), Times.Never);

    // the retried operation continues at the offset the partner received
    var result = await Push(record, transactionId);
    Assert.Equal(FederationJobResult.Success, result);
    Assert.Equal([0L, 1_048_576L, 1_048_576L, 2_097_152L], offsets);
  }

  [Fact]
  public async Task Execute_StopsWhenPartnerMakesNoProgress()
  {
    var (record, _) = AddRecordWithFile(2_500_000);
    client
      .Setup(c => c.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent?>(), null))
      .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(ReceiveResult.Pending(0)) });

    var result = await Push(record, Guid.NewGuid());

    Assert.False(result.Done);
    links.Verify(l => l.Create(It.IsAny<RecordLink>()), Times.Never);
  }

  [Fact]
  public async Task Execute_ContinuesAtOffsetOfPartner()
  {
    var (record, data) = AddRecordWithFile(2_500_000);
    var offsets = SimulatePartner(data, alreadyReceived: 2_000_000, conflictOffset: 2_000_000);

    var result = await Push(record, Guid.NewGuid());

    Assert.Equal(FederationJobResult.Success, result);
    Assert.Equal([2_000_000L, 2_000_010L], offsets);
  }

  [Fact]
  public async Task ReceiveDeletion_RemovesOnlyRecordsReceivedFromPartner()
  {
    var incoming = new RecordLink { TransactionId = Guid.NewGuid(), LocalRecordId = Guid.NewGuid(), Direction = SyncDirection.Incoming };
    var outgoing = new RecordLink { TransactionId = Guid.NewGuid(), LocalRecordId = Guid.NewGuid(), Direction = SyncDirection.Outgoing };
    links.Setup(l => l.GetByTransaction(peer.PeerId, incoming.TransactionId)).Returns(incoming);
    links.Setup(l => l.GetByTransaction(peer.PeerId, outgoing.TransactionId)).Returns(outgoing);
    var service = CreateService();

    Assert.True(await service.ReceiveDeletion(peer, incoming.TransactionId));
    Assert.False(await service.ReceiveDeletion(peer, outgoing.TransactionId));
    Assert.False(await service.ReceiveDeletion(peer, Guid.NewGuid()));

    recordService.Verify(r => r.DeleteRecord(incoming.LocalRecordId, true), Times.Once);
    recordService.Verify(r => r.DeleteRecord(outgoing.LocalRecordId, It.IsAny<bool>()), Times.Never);
  }

  [Fact]
  public async Task ReceiveUpdate_UpdatesMetadataAsOwner()
  {
    var link = new RecordLink { LinkId = Guid.NewGuid(), TransactionId = Guid.NewGuid(), LocalRecordId = Guid.NewGuid(), Direction = SyncDirection.Incoming };
    links.Setup(l => l.GetByTransaction(peer.PeerId, link.TransactionId)).Returns(link);

    var updated = await CreateService().ReceiveUpdate(peer, link.TransactionId, new SyncedRecord { Title = "New title", Artist = "New artist" }, null);

    Assert.True(updated);
    recordService.Verify(r => r.UpdateFromOwner(It.Is<Record>(rec => rec.RecordId == link.LocalRecordId && rec.Title == "New title" && rec.Artist == "New artist"), It.IsAny<byte[]>()), Times.Once);
    links.Verify(l => l.Touch(link.LinkId), Times.Once);
  }

  [Fact]
  public async Task Execute_TreatsMissingRecordOnPartnerAsDeleted()
  {
    var job = new FederationJob { Type = FederationJobType.DeleteRecord, TransactionId = Guid.NewGuid(), PeerId = peer.PeerId };
    client
      .Setup(c => c.Send(peer.Endpoint, peer.Instance, HttpMethod.Delete, $"records/{job.TransactionId}", null, null))
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

    var result = await CreateService().Execute(job, peer);

    Assert.Equal(FederationJobResult.Success, result);
  }

  [Fact]
  public async Task Execute_RetriesWhilePartnerIsNotLinked()
  {
    var notLinked = new Peer { PeerId = peer.PeerId, Instance = peer.Instance, State = PeerState.ResponsePending };

    var result = await CreateService().Execute(new FederationJob { Type = FederationJobType.PushRecord }, notLinked);

    Assert.False(result.Done);
    client.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Execute_SkipsPushOfRecordAlreadyLinked()
  {
    var record = AddRecord();
    links.Setup(l => l.GetByRecord(peer.PeerId, record.RecordId)).Returns(new RecordLink());

    var result = await CreateService().Execute(new FederationJob { Type = FederationJobType.PushRecord, RecordId = record.RecordId }, peer);

    Assert.Equal(FederationJobResult.Success, result);
    client.VerifyNoOtherCalls();
  }

  [Theory]
  [InlineData(HttpStatusCode.OK, true, false)]
  [InlineData(HttpStatusCode.InternalServerError, false, true)]
  [InlineData(HttpStatusCode.TooManyRequests, false, true)]
  [InlineData(HttpStatusCode.BadRequest, true, true)]
  [InlineData(HttpStatusCode.RequestEntityTooLarge, true, true)]
  public void FromResponse_RetriesOnlyTransientErrors(HttpStatusCode status, bool done, bool hasError)
  {
    var result = FederationJobResult.FromResponse(new HttpResponseMessage(status));

    Assert.Equal(done, result.Done);
    Assert.Equal(hasError, result.Error != null);
  }
}
