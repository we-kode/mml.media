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

public class SqlArtistRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : IArtistRepository
{
  public Artists List(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = contextFactory();
    var query = context.Artists
      .Where(ar => string.IsNullOrEmpty(filter) || EF.Functions.ILike(ar.Name, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(artist => artist.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))));
    }

    var count = query.Count();
    var artists = query
      .OrderBy(artist => artist.Name)
      .Skip(skip)
      .Take(take)
      .Select(artist => mapper.Map<Artist>(artist))
      .ToList();

    return new Artists
    {
      TotalCount = count,
      Items = artists
    };
  }

  public async Task TryRemove(string? artistName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(artistName))
    {
      return;
    }

    if (!context.Records.Any(rec => rec.Artist != null && rec.Artist.Name == artistName))
    {
      context.Artists.RemoveRange(context.Artists.Where(rec => rec.Name == artistName));
    }
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<Artist?> TryGetOrCreate(string? artistName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(artistName))
    {
      return null;
    }

    var artist = context.Artists.FirstOrDefault(art => art.Name == artistName);
    artist ??= new DBContext.Models.Artist
    {
      Name = artistName
    };

    if (artist.ArtistId == Guid.Empty)
    {
      context.Artists.Add(artist);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    return mapper.Map<Artist?>(artist);
  }
}
