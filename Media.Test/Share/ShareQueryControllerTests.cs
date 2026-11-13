using Media.API.Contracts;
using Media.API.Controllers;
using Media.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareQueryControllerTests
{
  private readonly Mock<IShareQueryRepository> repository = new();

  [Fact]
  public async Task Create_RejectsEmptyAndTooLargeFilters()
  {
    var controller = new ShareQueryController(repository.Object);

    Assert.IsType<BadRequestResult>((await controller.Create(new ShareSearch())).Result);
    Assert.IsType<BadRequestResult>((await controller.Create(new ShareSearch { Q = new string('x', 501) })).Result);
    Assert.IsType<BadRequestResult>((await controller.Create(new ShareSearch { Q = "text", Genre = [.. new string[1000]] })).Result);
    repository.Verify(r => r.GetOrCreate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
  }

  [Fact]
  public async Task Create_GivesEqualSearchesTheSameHash()
  {
    var hashes = new List<string>();
    repository.Setup(r => r.GetOrCreate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
      .Callback((string hash, string _, string _, DateTime _) => hashes.Add(hash))
      .ReturnsAsync("token");
    var controller = new ShareQueryController(repository.Object);

    var first = await controller.Create(new ShareSearch { Genre = ["b", "a"], Q = "text" });
    await controller.Create(new ShareSearch { Q = "text", Genre = ["a", "b"] });

    Assert.Equal("token", first.Value!.Token);
    Assert.Equal(2, hashes.Count);
    Assert.Equal(hashes[0], hashes[1]);
  }

  [Fact]
  public async Task Get_ReturnsTheStoredFiltersOrNotFound()
  {
    string? stored = null;
    repository.Setup(r => r.GetOrCreate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
      .Callback((string _, string json, string _, DateTime _) => stored = json)
      .ReturnsAsync("known");
    var controller = new ShareQueryController(repository.Object);
    await controller.Create(new ShareSearch { Genre = ["b", "a"], Q = "text", From = "2026-01-01" });
    repository.Setup(r => r.Find("known", It.IsAny<DateTime>())).Returns(() => stored);
    repository.Setup(r => r.Find("invalid", It.IsAny<DateTime>())).Returns("""{"Q":["text"]}""");

    var result = controller.Get("known").Value!;

    Assert.Equal(["a", "b"], result.Genre);
    Assert.Equal("text", result.Q);
    Assert.Equal("2026-01-01", result.From);
    Assert.IsType<NotFoundResult>(controller.Get("unknown").Result);
    Assert.IsType<NotFoundResult>(controller.Get("invalid").Result);
  }
}
