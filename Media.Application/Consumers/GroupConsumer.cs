using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Messages.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

namespace Media.Application.Consumers;

/// <summary>
/// Handles messages for changed groups
/// </summary>
public class GroupConsumer(IGroupRepository groupRepository) : IHandleMessages<GroupCreated>, IHandleMessages<GroupUpdated>, IHandleMessages<GroupDeleted>
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
    await groupRepository.Delete(context.Id).ConfigureAwait(false);
  }
}
