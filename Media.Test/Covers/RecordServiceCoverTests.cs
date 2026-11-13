using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Types;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Record = Media.Application.Models.Record;

namespace Media.Test.Covers;

public class RecordServiceCoverTests
{
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<ICoverLoader> covers = new();
  private readonly Mock<IFederationJobRepository> jobs = new();

  public RecordServiceCoverTests()
  {
    jobs.Setup(j => j.EnqueueForOutgoingLinks(It.IsAny<Guid>(), It.IsAny<FederationJobType>())).ReturnsAsync([]);
  }

  private RecordService CreateService() => new(records.Object, new Mock<IArtistRepository>().Object, new Mock<IAlbumRepository>().Object, new Mock<IGenreRepository>().Object, new Mock<ILanguageRepository>().Object, covers.Object, jobs.Object, new Mock<IShareRuleRepository>().Object, new Mock<IFederationAuditLogger>().Object);

  private Record AddRecord(string? cover, string owner = TestEnvironment.Instance)
  {
    var record = new Record(Guid.NewGuid(), "Title", owner) { Cover = cover };
    records.Setup(r => r.TryGetRecord(record.RecordId)).Returns(record);
    return record;
  }

  [Fact]
  public async Task Update_ReplacesCoverAndDeletesUnusedOldCover()
  {
    var record = AddRecord("old.jpg");
    byte[] cover = [0xFF, 0xD8, 0xFF, 0x01];

    await CreateService().Update(new Record(record.RecordId, "Title", string.Empty), cover);

    covers.Verify(c => c.Save(cover, cover.ToFileName()), Times.Once);
    records.Verify(r => r.Update(It.Is<Record>(u => u.Cover == cover.ToFileName()), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), false), Times.Once);
    covers.Verify(c => c.Delete("old.jpg"), Times.Once);
  }

  [Fact]
  public async Task Update_KeepsOldCoverUsedByAnotherRecord()
  {
    var record = AddRecord("shared.jpg");
    records.Setup(r => r.IsCoverUsed("shared.jpg")).Returns(true);

    await CreateService().Update(new Record(record.RecordId, "Title", string.Empty), [0xFF, 0xD8, 0xFF, 0x02]);

    covers.Verify(c => c.Delete(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task Update_RemovesCoverOnRequest()
  {
    var record = AddRecord("old.jpg");

    await CreateService().Update(new Record(record.RecordId, "Title", string.Empty), [], removeCover: true);

    records.Verify(r => r.Update(It.Is<Record>(u => u.Cover == null), It.IsAny<(Guid?, Guid?, Guid?, Guid?)>(), false), Times.Once);
    covers.Verify(c => c.Delete("old.jpg"), Times.Once);
  }

  [Fact]
  public async Task Update_KeepsCoverWithoutChange()
  {
    var record = AddRecord("old.jpg");

    await CreateService().Update(new Record(record.RecordId, "Title", string.Empty), []);

    covers.Verify(c => c.Delete(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task Update_DoesNotRemoveCoverOfForeignRecord()
  {
    var record = AddRecord("old.jpg", "beta");

    await CreateService().Update(new Record(record.RecordId, "Title", string.Empty), [], removeCover: true);

    covers.Verify(c => c.Delete(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task DeleteRecord_DeletesUnusedCover()
  {
    var record = AddRecord("old.jpg");

    await CreateService().DeleteRecord(record.RecordId);

    covers.Verify(c => c.Delete("old.jpg"), Times.Once);
  }

  [Theory]
  [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
  [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, "image/png")]
  [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38 }, null)]
  [InlineData(new byte[] { }, null)]
  public void ToContentType_DetectsSupportedImages(byte[] data, string? expected)
  {
    Assert.Equal(expected, data.ToContentType());
  }

  [Theory]
  [InlineData("AbCdEfGh-_123456.jpg", true)]
  [InlineData("../../etc/passwd", false)]
  [InlineData("AbCdEfGh-_123456.png", false)]
  public void IsCoverFileName_AcceptsOnlyGeneratedNames(string name, bool expected)
  {
    Assert.Equal(expected, name.IsCoverFileName());
  }
}
