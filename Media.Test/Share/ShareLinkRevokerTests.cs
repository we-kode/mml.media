using Media.API.Services;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareLinkRevokerTests
{
  private readonly Mock<IShareLinkRepository> links = new();
  private readonly Mock<IShareRuleRepository> rules = new();
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IAuthorizationClient> auth = new();
  private readonly List<Guid> groups = [Guid.NewGuid()];
  private readonly Guid allowed = Guid.NewGuid();
  private readonly Guid forbidden = Guid.NewGuid();
  private readonly Guid deleted = Guid.NewGuid();
  private IList<Guid>? revoked;

  public ShareLinkRevokerTests()
  {
    auth.Setup(a => a.GetGroups("client", false)).ReturnsAsync(groups);
    records.Setup(r => r.GetExistingIds(It.IsAny<IEnumerable<Guid>>()))
      .Returns((IEnumerable<Guid> ids) => [.. ids.Where(id => id != deleted)]);
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), groups))
      .Returns((IEnumerable<Guid> ids, IList<Guid> _) => [.. ids.Where(id => id == allowed)]);
    links.Setup(l => l.Revoke(It.IsAny<IList<Guid>>()))
      .Callback((IList<Guid> ids) => revoked = ids)
      .Returns(Task.CompletedTask);
  }

  private ShareLinkRevoker Create() => new(links.Object, rules.Object, records.Object, auth.Object);

  private static ShareLink Link(params Guid[] ids) => new() { Id = Guid.NewGuid(), CreatedBy = "client", RecordIds = ids };

  [Fact]
  public async Task RevokeUnallowed_RevokesOnlyLinksWithRecordsTheCreatorMayNotShare()
  {
    var keep = Link(allowed, deleted);
    var revoke = Link(allowed, forbidden);
    links.Setup(l => l.GetActiveOfClients(null, It.IsAny<DateTime>())).Returns([keep, revoke]);

    await Create().RevokeUnallowed();

    Assert.Equal([revoke.Id], revoked);
  }

  [Fact]
  public async Task RevokeUnallowed_KeepsLinksIfTheRightsCanNotBeLoaded()
  {
    auth.Setup(a => a.GetGroups("client", false)).ReturnsAsync((List<Guid>?)null);
    links.Setup(l => l.GetActiveOfClients("client", It.IsAny<DateTime>())).Returns([Link(forbidden)]);

    await Create().RevokeUnallowed("client");

    links.Verify(l => l.Revoke(It.IsAny<IList<Guid>>()), Times.Never);
  }

  [Fact]
  public async Task RevokeUnallowed_RevokesLinksOfDeletedClients()
  {
    auth.Setup(a => a.GetGroups("client", false)).ReturnsAsync([]);
    var link = Link(allowed);
    links.Setup(l => l.GetActiveOfClients("client", It.IsAny<DateTime>())).Returns([link]);
    rules.Setup(r => r.FilterShareable(It.IsAny<IEnumerable<Guid>>(), It.Is<IList<Guid>>(g => g.Count == 0))).Returns([]);

    await Create().RevokeUnallowed("client");

    Assert.Equal([link.Id], revoked);
  }

  [Fact]
  public async Task KeepIfAllowed_ChecksClientLinksAndKeepsAdminLinks()
  {
    var admin = Link(forbidden);
    admin.CreatedByAdmin = true;
    var client = Link(forbidden);

    Assert.True(await Create().KeepIfAllowed(admin));
    Assert.True(await Create().KeepIfAllowed(Link(allowed)));
    Assert.False(await Create().KeepIfAllowed(client));
    Assert.Equal([client.Id], revoked);
  }
}
