using AutoMapper;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure.Repositories;

public class SqlGroupRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : IGroupRepository
{
  private readonly Func<ApplicationDBContext> _contextFactory = contextFactory;
  private readonly IMapper _mapper = mapper;

  public Groups List(
    string? filter,
    int skip = Application.Constants.List.Skip,
    int take = Application.Constants.List.Take
  )
  {
    using var context = _contextFactory();
    var query = context.Groups
      .Where(group => string.IsNullOrEmpty(filter) ||
        EF.Functions.ILike(group.Name ?? "", $"%{filter}%")
      )
      .OrderBy(group => group.Name);

    var count = query.Count();
    var groups = query
      .Skip(skip)
      .Take(take == -1 ? count : take);

    return new Groups
    {
      TotalCount = count,
      Items = _mapper.ProjectTo<Group>(groups).ToList()
    };
  }

  public async Task Create(Group group)
  {
    var context = _contextFactory();
    var groups = context.Groups.FirstOrDefault(g => g.GroupId == group.Id);
    if (groups != null)
    {
      return;
    }

    groups = new DBContext.Models.Group
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
      context.Dispose();
      await Create(group);
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
