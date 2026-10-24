using Media.Application.Models.Federation;
using Media.Infrastructure.IO;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationUploadStoreTests
{
  private readonly FederationUploadStore store = new(new FederationOptions { UploadPath = TestEnvironment.CreateTempFolder() });
  private readonly Guid peerId = Guid.NewGuid();
  private readonly Guid transactionId = Guid.NewGuid();
  private readonly byte[] data = RandomNumberGenerator.GetBytes(1000);

  private string Hash => Convert.ToHexString(SHA256.HashData(data));

  private Task<UploadSession> Start(byte[]? cover = null) => store.Start(peerId, transactionId, new SyncedRecord { Title = "Title" }, data.Length, Hash, cover);

  private Task<(bool Accepted, long Offset)> Append(long offset, int length) => store.Append(peerId, transactionId, offset, new MemoryStream(data, (int)offset, length));

  [Fact]
  public async Task Append_ContinuesInterruptedTransfer()
  {
    await Start([1, 2, 3]);
    Assert.Equal((true, 400L), await Append(0, 400));

    // the sender restarts and gets the received bytes
    var resumed = await Start();
    Assert.Equal(400, resumed.Offset);
    Assert.Equal((true, 1000L), await Append(400, 600));

    var session = store.Get(peerId, transactionId)!;
    Assert.True(await store.IsComplete(session));
    Assert.Equal(data, File.ReadAllBytes(store.DataPath(session)));
    Assert.Equal([1, 2, 3], await store.ReadCover(session));
    Assert.Equal("Title", session.Metadata.Title);
  }

  [Fact]
  public async Task MoveData_NeverReplacesExistingFile()
  {
    await Start();
    await Append(0, data.Length);
    var session = store.Get(peerId, transactionId)!;
    var target = Path.Combine(TestEnvironment.CreateTempFolder(), "record");
    File.WriteAllBytes(target, [1, 2, 3]);

    Assert.False(store.MoveData(session, target));
    Assert.Equal([1, 2, 3], File.ReadAllBytes(target));

    File.Delete(target);
    Assert.True(store.MoveData(session, target));
    Assert.Equal(data, File.ReadAllBytes(target));
  }

  [Fact]
  public async Task Append_RejectsWrongOffset()
  {
    await Start();
    await Append(0, 300);

    Assert.Equal((false, 300L), await Append(100, 200));
    Assert.Equal((false, 300L), await Append(500, 200));
    Assert.Equal(300, store.Get(peerId, transactionId)!.Offset);
  }

  [Fact]
  public async Task Append_RejectsMoreBytesThanAnnounced()
  {
    await store.Start(peerId, transactionId, new SyncedRecord(), 500, Hash, null);
    await Append(0, 400);

    var exception = await Assert.ThrowsAsync<FederationException>(() => Append(400, 200));

    Assert.Equal(FederationErrors.InvalidRecord, exception.Code);
    Assert.Equal(400, store.Get(peerId, transactionId)!.Offset);
  }

  [Fact]
  public async Task Append_RejectsUnknownTransfer()
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => Append(0, 10));

    Assert.Equal(FederationErrors.NotFound, exception.Code);
  }

  [Fact]
  public async Task Start_RestartsTransferOfOtherFile()
  {
    await Start();
    await Append(0, 500);

    var restarted = await store.Start(peerId, transactionId, new SyncedRecord(), 2000, new string('0', 64), null);

    Assert.Equal(0, restarted.Offset);
    Assert.Equal(2000, restarted.Size);
  }

  [Fact]
  public async Task IsComplete_DetectsCorruptedFile()
  {
    await store.Start(peerId, transactionId, new SyncedRecord(), data.Length, new string('0', 64), null);
    await Append(0, data.Length);
    var session = store.Get(peerId, transactionId)!;

    Assert.False(await store.IsComplete(session));

    store.Reset(session);
    Assert.Equal(0, store.Get(peerId, transactionId)!.Offset);
  }

  [Fact]
  public async Task RemoveStale_RemovesOnlyTransfersWithoutProgress()
  {
    await Start();
    var otherPeer = Guid.NewGuid();
    await store.Start(otherPeer, Guid.NewGuid(), new SyncedRecord(), 10, Hash, null);
    Assert.Equal(1, store.Count(peerId));

    store.RemoveStale(DateTime.UtcNow.AddMinutes(-1));
    Assert.Equal(1, store.Count(peerId));

    store.RemoveStale(DateTime.UtcNow.AddMinutes(1));
    Assert.Equal(0, store.Count(peerId));
    Assert.Equal(0, store.Count(otherPeer));
  }

  [Fact]
  public async Task RemoveAll_RemovesTransfersOfPartner()
  {
    await Start();
    await store.Start(peerId, Guid.NewGuid(), new SyncedRecord(), 10, Hash, null);
    Assert.Equal(2, store.Count(peerId));

    store.RemoveAll(peerId);

    Assert.Equal(0, store.Count(peerId));
    Assert.Null(store.Get(peerId, transactionId));
  }
}
