using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Record = Media.Application.Models.Record;

namespace Media.Test.Federation;

public class RecordServiceFederationTests
{
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IArtistRepository> artists = new();
  private readonly Mock<IAlbumRepository> albums = new();
  private readonly Mock<IGenreRepository> genres = new();
  private readonly Mock<ILanguageRepository> languages = new();
  private readonly Mock<ICoverLoader> covers = new();
  private readonly Mock<IFederationJobRepository> jobs = new();
  private readonly Mock<IFederationAuditLogger> audit = new();

  private readonly List<AuditEntry> pending = [new AuditEntry { Status = AuditStatus.Pending }];

  public RecordServiceFederationTests()
  {
    jobs.Setup(j => j.EnqueueForOutgoingLinks(It.IsAny<Guid>(), It.IsAny<FederationJobType>())).ReturnsAsync(pending);
  }

  private RecordService CreateService() => new(records.Object, artists.Object, albums.Object, genres.Object, languages.Object, covers.Object, jobs.Object, audit.Object);

  private Record AddRecord(string owner)
  {
    var record = new Record(Guid.NewGuid(), "Title", owner) { Artist = "Artist" };
    records.Setup(r => r.TryGetRecord(record.RecordId)).Returns(record);
    return record;
  }

  [Fact]
  public async Task Update_SendsChangedMetadataOfOwnRecordToPartners()
  {
    var record = AddRecord(TestEnvironment.Instance);

    await CreateService().Update(new Record(record.RecordId, "Changed", string.Empty), []);

    records.Verify(r => r.Update(It.Is<Record>(u => u.Title == "Changed"), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), false), Times.Once);
    jobs.Verify(j => j.EnqueueForOutgoingLinks(record.RecordId, FederationJobType.UpdateRecord), Times.Once);
    audit.Verify(a => a.Announce(pending), Times.Once);
  }

  [Fact]
  public async Task Update_OnlyChangesGroupsOfForeignRecord()
  {
    var record = AddRecord("beta");

    await CreateService().Update(new Record(record.RecordId, "Changed", string.Empty) { Artist = "Other" }, []);

    records.Verify(r => r.Update(It.IsAny<Record>(), It.Is<(Guid?, Guid?, Guid?, Guid?)>(refs => refs.Item1 == null && refs.Item2 == null && refs.Item3 == null && refs.Item4 == null), false), Times.Once);
    artists.Verify(a => a.TryGetOrCreate(It.IsAny<string?>()), Times.Never);
    jobs.Verify(j => j.EnqueueForOutgoingLinks(It.IsAny<Guid>(), It.IsAny<FederationJobType>()), Times.Never);
  }

  [Fact]
  public async Task Update_KeepsCoverWithoutNewCover()
  {
    var record = AddRecord(TestEnvironment.Instance);
    record.Cover = "cover.jpg";

    await CreateService().Update(new Record(record.RecordId, "Changed", string.Empty), []);

    records.Verify(r => r.Update(It.Is<Record>(u => u.Cover == "cover.jpg"), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), false), Times.Once);
  }

  [Fact]
  public async Task UpdateFromOwner_RemovesCoverWithoutCover()
  {
    var record = AddRecord("beta");
    record.Cover = "cover.jpg";

    await CreateService().UpdateFromOwner(new Record(record.RecordId, "Changed", "beta"), []);

    records.Verify(r => r.Update(It.Is<Record>(u => u.Cover == null), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), true), Times.Once);
  }

  [Fact]
  public async Task UpdateFromOwner_KeepsLocalGroupsAndLock()
  {
    var record = AddRecord("beta");
    record.Locked = true;
    record.Groups = [new Group(Guid.NewGuid(), "Local", false)];

    await CreateService().UpdateFromOwner(new Record(record.RecordId, "Changed", "beta"), []);

    records.Verify(r => r.Update(It.Is<Record>(u => u.Title == "Changed" && u.Locked && u.Groups == record.Groups), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), true), Times.Once);
    jobs.Verify(j => j.EnqueueForOutgoingLinks(It.IsAny<Guid>(), It.IsAny<FederationJobType>()), Times.Never);
  }

  [Fact]
  public async Task DeleteRecord_InformsPartnersOnlyForOwnRecords()
  {
    var own = AddRecord(TestEnvironment.Instance);
    var foreign = AddRecord("beta");
    var service = CreateService();

    await service.DeleteRecord(own.RecordId);
    await service.DeleteRecord(foreign.RecordId);

    jobs.Verify(j => j.EnqueueForOutgoingLinks(own.RecordId, FederationJobType.DeleteRecord), Times.Once);
    jobs.Verify(j => j.EnqueueForOutgoingLinks(foreign.RecordId, It.IsAny<FederationJobType>()), Times.Never);
    records.Verify(r => r.RemoveRecord(own.RecordId), Times.Once);
    records.Verify(r => r.RemoveRecord(foreign.RecordId), Times.Once);
  }
}
