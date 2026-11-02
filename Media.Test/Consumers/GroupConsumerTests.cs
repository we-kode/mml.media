using Media.Application.Consumers;
using Media.Application.Contracts.Repositories;
using Messages.Events;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Consumers;

public class GroupConsumerTests
{
  [Fact]
  public async Task GroupDeleted_RemovesNotificationsOnlyTargetingTheGroupBeforeTheGroup()
  {
    var calls = new System.Collections.Generic.List<string>();
    var groups = new Mock<IGroupRepository>();
    var notifications = new Mock<INotificationRepository>();
    var id = Guid.NewGuid();
    notifications.Setup(n => n.RemoveTargetingOnly(id)).Callback(() => calls.Add("notifications")).Returns(Task.CompletedTask);
    groups.Setup(g => g.Delete(id)).Callback(() => calls.Add("group")).Returns(Task.CompletedTask);

    await new GroupConsumer(groups.Object, notifications.Object).Handle(new GroupDeleted(id));

    Assert.Equal(["notifications", "group"], calls);
  }
}
