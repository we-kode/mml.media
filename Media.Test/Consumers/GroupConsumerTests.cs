using Media.Application.Consumers;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Messages.Events;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Consumers;

public class GroupConsumerTests
{
  [Fact]
  public async Task GroupDeleted_RemovesNotificationsAndRulesOnlyTargetingTheGroupBeforeTheGroupAndRevokesLinks()
  {
    var calls = new System.Collections.Generic.List<string>();
    var groups = new Mock<IGroupRepository>();
    var notifications = new Mock<INotificationRepository>();
    var rules = new Mock<IShareRuleRepository>();
    var revoker = new Mock<IShareLinkRevoker>();
    var id = Guid.NewGuid();
    notifications.Setup(n => n.RemoveTargetingOnly(id)).Callback(() => calls.Add("notifications")).Returns(Task.CompletedTask);
    rules.Setup(r => r.RemoveTargetingOnly(id)).Callback(() => calls.Add("rules")).Returns(Task.CompletedTask);
    groups.Setup(g => g.Delete(id)).Callback(() => calls.Add("group")).Returns(Task.CompletedTask);
    revoker.Setup(r => r.RevokeUnallowed(null)).Callback(() => calls.Add("links")).Returns(Task.CompletedTask);

    await new GroupConsumer(groups.Object, notifications.Object, rules.Object, revoker.Object).Handle(new GroupDeleted(id));

    Assert.Equal(["notifications", "rules", "group", "links"], calls);
  }
}
