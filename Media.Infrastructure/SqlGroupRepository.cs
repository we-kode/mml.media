using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Media.DBContext.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure
{
  public class SqlGroupRepository : IGroupRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;
    private readonly IMapper mapper;

    public SqlGroupRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper)
    {
      _contextFactory = contextFactory;
      this.mapper = mapper;
    }

    public async Task Create(Group group)
    {
      var context = _contextFactory();
      var groups = context.Groups.FirstOrDefault(g => g.GroupId == group.Id);
      if (groups != null)
      {
        return;
      }

      groups = new Groups
      {
        GroupId = group.Id,
        IsDefault = group.IsDefault,
        Name = group.Name
      };
      context.Add(groups);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task Delete(Guid id)
    {
      var context = _contextFactory();
      var group = context.Groups.FirstOrDefault(g => g.GroupId == id);
      if (group == null)
      {
        return;
      }
      context.Remove(group);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public Guid? GetDefaultGroup()
    {
      var context = _contextFactory();
      return context.Groups.FirstOrDefault(g => g.IsDefault)?.GroupId;
    }

    public async Task Update(Group group)
    {
      var context = _contextFactory();
      var groups = context.Groups.FirstOrDefault(g => g.GroupId == group.Id);
      if (groups == null)
      {
        return;
      }
      groups.Name = group.Name;
      groups.IsDefault = group.IsDefault;
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
  }
}
