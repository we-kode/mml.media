
using AutoMapper;
using MassTransit;
using Media.Application.Contracts;
using Media.Application.Models;
using Messages;
using System.Threading.Tasks;

namespace Media.Application.Consumers;

/// <summary>
/// Handles messages for chnaged groups
/// </summary>
public class GroupConsumer : IConsumer<GroupCreated>, IConsumer<GroupUpdated>, IConsumer<GroupDeleted>
{
  private readonly IGroupRepository groupRepository;
  private readonly IMapper mapper;

  public GroupConsumer(IGroupRepository groupRepository, IMapper mapper)
  {
    this.groupRepository = groupRepository;
    this.mapper = mapper;
  }

  public async Task Consume(ConsumeContext<GroupCreated> context)
  {
    await groupRepository.Create(mapper.Map<Group>(context.Message)).ConfigureAwait(false);
  }

  public async Task Consume(ConsumeContext<GroupUpdated> context)
  {
    await groupRepository.Update(mapper.Map<Group>(context.Message)).ConfigureAwait(false);
  }

  public async Task Consume(ConsumeContext<GroupDeleted> context)
  {
    await groupRepository.Delete(context.Message.Id).ConfigureAwait(false);
  }
}
