using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using Messages.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

namespace Media.Application.Consumers;

/// <summary>
/// Handles messages for changed groups
/// </summary>
public class GroupConsumer(IGroupRepository groupRepository, INotificationRepository notificationRepository, IShareRuleRepository shareRuleRepository, IShareLinkRevoker shareLinkRevoker) : IHandleMessages<GroupCreated>, IHandleMessages<GroupUpdated>, IHandleMessages<GroupDeleted>
{
  public async Task Handle(GroupCreated context)
  {
    await groupRepository.Create(new Group(context.Id, context.Name, context.IsDefault)).ConfigureAwait(false);
  }

  public async Task Handle(GroupUpdated context)
  {
    await groupRepository.Update(new Group(context.Id, context.Name, context.IsDefault)).ConfigureAwait(false);
  }

  public async Task Handle(GroupDeleted context)
  {
    await notificationRepository.RemoveTargetingOnly(context.Id).ConfigureAwait(false);
    await shareRuleRepository.RemoveTargetingOnly(context.Id).ConfigureAwait(false);
    await groupRepository.Delete(context.Id).ConfigureAwait(false);
    await shareLinkRevoker.RevokeUnallowed().ConfigureAwait(false);
  }
}
