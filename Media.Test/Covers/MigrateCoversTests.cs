using Media.API.HostedServices;
using Media.Application.Contracts.IO;
using Media.Application.Extensions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Covers;

public class MigrateCoversTests
{
  private readonly Mock<ICoverLoader> covers = new();

  private MigrateCovers CreateService() => new(() => null!, covers.Object);

  [Fact]
  public async Task MigrateCover_StoresBase64CoverAsFile()
  {
    byte[] cover = [0xFF, 0xD8, 0xFF, 0x01];

    var fileName = await CreateService().MigrateCover(Convert.ToBase64String(cover));

    Assert.Equal(cover.ToFileName(), fileName);
    covers.Verify(c => c.Save(cover, cover.ToFileName()), Times.Once);
  }

  [Fact]
  public async Task MigrateCover_KeepsCoverFileName()
  {
    byte[] cover = [0xFF, 0xD8, 0xFF, 0x02];

    Assert.Null(await CreateService().MigrateCover(cover.ToFileName()));
    covers.Verify(c => c.Save(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task MigrateCover_IgnoresInvalidCover()
  {
    Assert.Null(await CreateService().MigrateCover("no base64!"));
    covers.Verify(c => c.Save(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
  }
}
