using Media.API.Contracts;
using Media.API.Controllers;
using Media.API.Services;
using Media.API.Share;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.Application.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareControllerTests
{
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IShareRuleRepository> rules = new();
  private readonly Mock<IShareLinkRepository> links = new();
  private readonly Mock<ISettingRepository> settings = new();
  private readonly Mock<IAuthorizationClient> auth = new();
  private readonly ShareOptions options = new() { MaxRecordsPerLink = 5 };
  private readonly List<Guid> groups = [Guid.NewGuid()];
  private DateTime createdExpires;
  private bool createdSingleUse;
  private string? createdHash;

  public ShareControllerTests()
  {
    settings.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>())).Returns((string _, string d) => d);
    auth.Setup(a => a.GetGroups("client", true)).ReturnsAsync(groups);
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IList<Guid>>())).Returns((IEnumerable<Guid> ids, IList<Guid> _) => [.. ids]);
    links.Setup(l => l.Create(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IList<Guid>>()))
      .Callback((string hash, DateTime expires, bool single, string _, bool _, IList<Guid> _) =>
      {
        createdHash = hash;
        createdExpires = expires;
        createdSingleUse = single;
      })
      .Returns(Task.CompletedTask);
  }

  private ShareController Create(bool admin)
  {
    var claims = new List<Claim> { new("sub", "client") };
    if (admin)
    {
      claims.Add(new Claim("role", "Admin"));
    }

    return new ShareController(records.Object, rules.Object, links.Object, settings.Object, auth.Object, Options.Create(options))
    {
      ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } },
    };
  }

  [Fact]
  public async Task Check_ClientCountsOnlyRecordsAllowedByRules()
  {
    var allowed = Guid.NewGuid();
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.Is<IList<Guid>>(g => g.SequenceEqual(groups)))).Returns([allowed]);

    var result = await Create(false).Check(new ShareRequest { Records = [allowed, Guid.NewGuid()] });

    Assert.Equal(1, result.Value!.Shareable);
    Assert.Equal(2, result.Value.Total);
    Assert.Equal([allowed], result.Value.ShareableRecords);
    Assert.Equal(options.MaxRecordsPerLink, result.Value.Limit);
  }

  [Fact]
  public async Task Check_AdminMayShareAllExistingRecords()
  {
    var id = Guid.NewGuid();
    records.Setup(r => r.GetExistingIds(It.IsAny<IEnumerable<Guid>>())).Returns([id]);

    var result = await Create(true).Check(new ShareRequest { Records = [id, Guid.NewGuid()] });

    Assert.Equal(1, result.Value!.Shareable);
    Assert.Equal(2, result.Value.Total);
    Assert.Equal([id], result.Value.ShareableRecords);
    rules.Verify(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IList<Guid>>()), Times.Never);
  }

  [Fact]
  public async Task Check_EmptyRequestIsRejected()
  {
    var result = await Create(false).Check(new ShareRequest());

    Assert.IsType<BadRequestResult>(result.Result);
  }
  [Fact]
  public async Task Create_ClientWithoutRuleIsForbiddenAndNoLinkIsCreated()
  {
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IList<Guid>>())).Returns([]);

    var result = await Create(false).Create(new ShareRequest { Records = [Guid.NewGuid()] });

    Assert.IsType<ForbidResult>(result.Result);
    Assert.Null(createdHash);
  }

  [Fact]
  public async Task Create_SharesOnlyAllowedRecordsAndReportsSkipped()
  {
    var allowed = Guid.NewGuid();
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IList<Guid>>())).Returns([allowed]);

    var result = await Create(false).Create(new ShareRequest { Records = [allowed, Guid.NewGuid(), Guid.NewGuid()] });

    var response = result.Value!;
    Assert.Equal(1, response.Shared);
    Assert.Equal(2, response.Skipped);
    Assert.Equal(ShareTokens.Hash(response.Token), createdHash);
  }

  [Fact]
  public async Task Create_ClientCannotChooseValidityUnlessAllowed()
  {
    await Create(false).Create(new ShareRequest { Records = [Guid.NewGuid()], ValidForHours = 1, SingleUse = false });

    Assert.True(createdSingleUse);
    Assert.True(createdExpires > DateTime.UtcNow.AddDays(89));
  }

  [Fact]
  public async Task Create_ClientChoosesValidityIfAllowed()
  {
    settings.Setup(s => s.Get("Share.UserSettingsAllowed", It.IsAny<string>())).Returns("True");

    await Create(false).Create(new ShareRequest { Records = [Guid.NewGuid()], ValidForHours = 1, SingleUse = false });

    Assert.False(createdSingleUse);
    Assert.True(createdExpires < DateTime.UtcNow.AddHours(2));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(ShareSettings.MaxValidForHours + 1)]
  public async Task Create_AdminValidityOutOfRangeIsRejected(int hours)
  {
    records.Setup(r => r.GetExistingIds(It.IsAny<IEnumerable<Guid>>())).Returns((IEnumerable<Guid> ids) => [.. ids]);

    var result = await Create(true).Create(new ShareRequest { Records = [Guid.NewGuid()], ValidForHours = hours });

    Assert.IsType<BadRequestResult>(result.Result);
    Assert.Null(createdHash);
  }

  [Fact]
  public async Task Create_MoreRecordsThanConfiguredPerLinkAreRejected()
  {
    var ids = Enumerable.Range(0, options.MaxRecordsPerLink + 1).Select(_ => Guid.NewGuid()).ToList();

    var check = await Create(false).Check(new ShareRequest { Records = ids });
    var result = await Create(false).Create(new ShareRequest { Records = ids });
    var allowed = await Create(false).Create(new ShareRequest { Records = ids[..options.MaxRecordsPerLink] });

    Assert.Equal(options.MaxRecordsPerLink, check.Value!.Limit);
    Assert.IsType<BadRequestResult>(result.Result);
    Assert.Equal(options.MaxRecordsPerLink, allowed.Value!.Shared);
  }

  [Fact]
  public async Task Capability_FollowsRulesOfClientGroups()
  {
    rules.Setup(r => r.HasRules(It.Is<IList<Guid>>(g => g.SequenceEqual(groups)))).Returns(true);

    Assert.True(await Create(false).Capability());
    Assert.True(await Create(true).Capability());
  }

  [Fact]
  public void GetLinks_ReturnsOnlyLinksOfTheRequester()
  {
    var info = new ShareLinkInfo(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1), true, 3);
    links.Setup(l => l.GetActive("client", It.IsAny<DateTime>())).Returns([info]);

    Assert.Equal([info], Create(true).GetLinks());
  }

  [Fact]
  public async Task DeleteLink_DeletesLinkOfTheRequesterOnly()
  {
    var id = Guid.NewGuid();
    links.Setup(l => l.Delete(id, "client")).ReturnsAsync(true);

    Assert.IsType<OkResult>(await Create(false).DeleteLink(id));
    Assert.IsType<NotFoundResult>(await Create(false).DeleteLink(Guid.NewGuid()));
  }
}
