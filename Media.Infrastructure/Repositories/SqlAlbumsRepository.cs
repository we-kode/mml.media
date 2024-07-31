using AutoMapper;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

class SqlAlbumsRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : IAlbumsRepository {
  public Albums List(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Media.Application.Constants.List.Skip, int take = Media.Application.Constants.List.Take)
  {
    using var context = contextFactory();
    var query = context.Albums
     .Where(al => string.IsNullOrEmpty(filter) || EF.Functions.ILike(al.AlbumName, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(album => album.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))));
    }

    var count = query.Count();
    var albums = query
      .OrderBy(album => album.AlbumName)
      .Skip(skip)
      .Take(take)
      .Select(album => mapper.Map<Album>(album))
      .ToList();

    return new Albums
    {
      TotalCount = count,
      Items = albums
    };
  }
}
