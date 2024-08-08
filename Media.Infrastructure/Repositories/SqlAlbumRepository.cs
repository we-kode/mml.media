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

public class SqlAlbumRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : IAlbumRepository {
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

  public async Task TryRemove(string? albumName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(albumName))
    {
      return;
    }

    if (!context.Records.Any(rec => rec.Album != null && rec.Album.AlbumName == albumName))
    {
      context.Albums.RemoveRange(context.Albums.Where(rec => rec.AlbumName == albumName));
    }
    
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<Album?> TryGetOrCreate(string? albumName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(albumName))
    {
      return null;
    }

    var album = context.Albums.FirstOrDefault(a => a.AlbumName == albumName);
    album ??= new DBContext.Models.Album
    {
      AlbumName = albumName
    };

    if (album.AlbumId == Guid.Empty)
    {
      context.Albums.Add(album);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    return mapper.Map<Album?>(album);
  }
}
