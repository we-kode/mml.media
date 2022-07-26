using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Media.DBContext.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure
{
  public class SqlGroupRepository : IGroupRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;

    public SqlGroupRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper)
    {
      _contextFactory = contextFactory;
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

    public IList<Guid>? GetDefaultGroups()
    {
      var context = _contextFactory();
      return context.Groups.Where(g => g.IsDefault).Select(g => g.GroupId).ToList();
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

    public async Task<bool> GroupExists(Guid id)
    {
      using var context = _contextFactory();
      return await context.Groups
        .AnyAsync(group => group.GroupId == id)
        .ConfigureAwait(false);
    }
  }
}
