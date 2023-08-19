using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Media.Infrastructure;

public class SqlLivestreamRepository : ILivestreamRepository
{
  private readonly Func<ApplicationDBContext> _contextFactory;
  private readonly IMapper mapper;
  private readonly IGroupRepository _groupRepository;

  public SqlLivestreamRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper, IGroupRepository groupRepository)
  {
    _contextFactory = contextFactory;
    this.mapper = mapper;
    _groupRepository = groupRepository;
  }

  public async Task Delete(Guid id)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();

    var item = context.Livestreams.FirstOrDefault(stream => stream.RecordId == id);
    if (item != null)
    {
      context.Remove(item);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
    scope.Complete();
  }

  public bool Exists(Guid id)
  {
    using var context = _contextFactory();
    var item = context.Livestreams.FirstOrDefault(stream => stream.RecordId == id);
    return item != null;
  }

  public Livestreams List(string? filter, bool filterByGroups, IList<Guid> clientGroups, int skip, int take)
  {
    using var context = _contextFactory();
    var query = context.Livestreams
      .Include(stream => stream.Groups)
      .Where(ar => string.IsNullOrEmpty(filter) || EF.Functions.ILike(ar.Title, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(elem => elem.Groups.Any(g => clientGroups.Contains(g.GroupId)));
    }

    var count = query.Count();
    var items = query
      .OrderBy(elem => elem.Title)
      .Skip(skip)
      .Take(take)
      .Select(elem => mapper.Map<Livestream>(elem))
      .ToList();

    return new Livestreams
    {
      TotalCount = count,
      Items = items
    };
  }

  public LivestreamSettings Load(Guid id)
  {
    using var context = _contextFactory();
    var item = context.Livestreams.Include(stream => stream.Groups).First(stream => stream.RecordId == id);
    return mapper.Map<LivestreamSettings>(item);
  }

  public async Task Update(LivestreamSettings value)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();
    var item = context.Livestreams
      .Include(rec => rec.Groups)
      .FirstOrDefault(stream => stream.RecordId == value.RecordId);
    if (item == null)
    {
      item = new DBContext.Models.Livestreams();
      context.Livestreams.Add(item);
    }

    item.Title = value.Title;
    item.Url = value.Url;

    await context.SaveChangesAsync().ConfigureAwait(false);

    // update groups
    var addedGroups = value.Groups
       .Where(g => _groupRepository.GroupExists(g.Id).GetAwaiter().GetResult())
       .Where(g => !item.Groups.Any(rg => rg.GroupId == g.Id))
       .Select(g => new DBContext.Models.Groups
       {
         GroupId = g.Id,
         Name = g.Name,
         IsDefault = g.IsDefault
       })
       .ToArray();

    var deletedGroups = item.Groups
      .Where(g => !value.Groups.Any(rg => rg.Id == g.GroupId))
      .ToArray();

    foreach (var addedGroup in addedGroups)
    {
      item.Groups.Add(addedGroup);
    }

    foreach (var deletedGroup in deletedGroups)
    {
      item.Groups.Remove(deletedGroup);
    }

    await context.SaveChangesAsync().ConfigureAwait(false);
    scope.Complete();
  }
}
