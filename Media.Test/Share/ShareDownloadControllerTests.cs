using Media.API.Controllers;
using Media.API.Services;
using Media.API.Share;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using Media.Application.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareDownloadControllerTests : IDisposable
{
  private const string Token = "secret-token";
  private readonly Mock<IShareLinkRepository> links = new();
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IShareLinkRevoker> revoker = new();
  private readonly string folder = TestEnvironment.CreateTempFolder();
  private readonly MemoryStream body = new();

  public void Dispose()
  {
    Directory.Delete(folder, true);
    GC.SuppressFinalize(this);
  }

  private ShareDownloadController Create()
  {
    return new ShareDownloadController(links.Object, new ShareArchiveService(records.Object), revoker.Object, new SharePages(new ShareStyling()))
    {
      ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Response = { Body = body } } },
    };
  }

  private ShareLink Link(bool singleUse, params Guid[] ids)
  {
    var link = new ShareLink { Id = Guid.NewGuid(), SingleUse = singleUse, RecordIds = ids };
    links.Setup(l => l.Find(ShareTokens.Hash(Token), It.IsAny<DateTime>())).Returns(link);
    links.Setup(l => l.TryUse(link.Id, It.IsAny<DateTime>())).ReturnsAsync(true);
    revoker.Setup(r => r.KeepIfAllowed(link)).ReturnsAsync(true);
    return link;
  }

  private Media.Application.Models.Record AddRecord(string title, string content)
  {
    var record = new Media.Application.Models.Record(Guid.NewGuid(), title, "alpha");
    var path = Path.Combine(folder, record.RecordId.ToString("N"));
    File.WriteAllText(path, content);
    records.Setup(r => r.StreamRecord(record.RecordId)).Returns(() => new RecordStream("audio/mpeg", File.OpenRead(path)) { FileName = title });
    return record;
  }

  [Fact]
  public async Task Preview_DoesNotUseUpTheLink()
  {
    Link(true, Guid.NewGuid());

    var result = await Create().Preview(Token);

    Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    links.Verify(l => l.TryUse(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
  }

  [Fact]
  public async Task Preview_IsGenericAndContainsNoTitles()
  {
    var record = AddRecord("Secret title", "x");
    Link(false, record.RecordId);

    var page = (await Create().Preview(Token)).Content!;

    Assert.DoesNotContain("Secret title", page);
    Assert.Contains("data-n=\"1\"", page);
    records.Verify(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>()), Times.Never);
  }

  [Fact]
  public async Task Preview_UnknownLinkIsGone()
  {
    Assert.Equal(StatusCodes.Status410Gone, (await Create().Preview("unknown")).StatusCode);
  }

  [Fact]
  public async Task RevokedLinkIsGoneAndNotUsed()
  {
    var link = Link(true, Guid.NewGuid());
    revoker.Setup(r => r.KeepIfAllowed(link)).ReturnsAsync(false);

    Assert.Equal(StatusCodes.Status410Gone, (await Create().Preview(Token)).StatusCode);
    var download = Assert.IsType<ContentResult>(await Create().Download(Token));
    Assert.Equal(StatusCodes.Status410Gone, download.StatusCode);
    links.Verify(l => l.TryUse(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
  }

  [Fact]
  public async Task Download_ExpiredOrUsedLinkIsGone()
  {
    var link = Link(true, Guid.NewGuid());
    links.Setup(l => l.TryUse(link.Id, It.IsAny<DateTime>())).ReturnsAsync(false);

    var result = await Create().Download(Token);

    var page = Assert.IsType<ContentResult>(result);
    Assert.Equal(StatusCodes.Status410Gone, page.StatusCode);
    Assert.DoesNotContain("secret-token", page.Content);
  }

  [Fact]
  public async Task Download_SingleRecordIsAudioFileNamedByTitle()
  {
    var record = AddRecord("Sermon: part 1", "audio");
    Link(false, record.RecordId);
    records.Setup(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>())).Returns([record]);

    var result = await Create().Download(Token);

    var file = Assert.IsType<FileStreamResult>(result);
    Assert.Equal("audio/mpeg", file.ContentType);
    Assert.Equal("Sermon part 1.mp3", file.FileDownloadName);
    await file.FileStream.DisposeAsync();
  }

  [Fact]
  public async Task Download_SeveralRecordsAreZippedWithUniqueTitleNames()
  {
    var first = AddRecord("Song", "one");
    var second = AddRecord("Song", "two");
    var third = AddRecord("Other", "three");
    Link(true, first.RecordId, second.RecordId, third.RecordId);
    records.Setup(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>())).Returns([first, second, third]);

    var controller = Create();
    var result = await controller.Download(Token);

    Assert.IsType<EmptyResult>(result);
    Assert.Contains($"Records_{DateTime.UtcNow:yyyy-MM-dd}.zip", controller.Response.Headers.ContentDisposition.ToString());
    body.Position = 0;
    using var archive = new ZipArchive(body);
    Assert.Equal(["Other.mp3", "Song (2).mp3", "Song.mp3"], archive.Entries.Select(e => e.FullName).Order().ToArray());
  }

  [Fact]
  public async Task Download_SkipsRecordsWithoutFile()
  {
    var existing = AddRecord("Present", "x");
    var missing = new Media.Application.Models.Record(Guid.NewGuid(), "Missing", "alpha");
    records.Setup(r => r.StreamRecord(missing.RecordId)).Throws<FileNotFoundException>();
    Link(false, existing.RecordId, missing.RecordId);
    records.Setup(r => r.GetRecordsByIds(It.IsAny<IEnumerable<Guid>>())).Returns([existing, missing]);

    await Create().Download(Token);

    body.Position = 0;
    using var archive = new ZipArchive(body);
    Assert.Equal(["Present.mp3"], archive.Entries.Select(e => e.FullName).ToArray());
  }
}
