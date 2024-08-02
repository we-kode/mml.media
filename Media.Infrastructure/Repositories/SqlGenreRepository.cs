
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

public class SqlGenreRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : IGenreRepository
{
  public int? Bitrate(string genreName)
  {
    using var context = contextFactory();
    return context.Genres.FirstOrDefault(genre => genre.Name == genreName)?.Bitrate;
  }

  public GenreBitrates Bitrates()
  {
    using var context = contextFactory();
    var genres = context.Genres
      .Where(genre => genre.Bitrate.HasValue)
      .OrderBy(genre => genre.Name)
      .Select(genre => mapper.Map<GenreBitrate>(genre));

    return new GenreBitrates
    {
      TotalCount = genres.Count(),
      Items = [..genres]
    };
  }

  public void DeleteBitrate(Guid genreId)
  {
    using var context = contextFactory();
    var genre = context.Genres
      .Include(g => g.Records)
      .FirstOrDefault(g => g.GenreId == genreId);

    if (genre == null)
    {
      return;
    }

    if (genre.Records.Count == 0)
    {
      context.Remove(genre);
    }
    else
    {
      genre.Bitrate = null;
    }

    context.SaveChanges();
  }

  public async Task UpdateBitrate(string checksum, int bitrate)
  {
    using var context = contextFactory();
    var record = context.Records.FirstOrDefault(r => r.Checksum == checksum);
    if (record == null)
    {
      return;
    }

    record.Bitrate = bitrate;
    await context.SaveChangesAsync();
  }

  public async Task UpdateBitrates(List<GenreBitrate> bitrates)
  {
    using var context = contextFactory();
    foreach (var bitrate in bitrates)
    {
      if (!bitrate.Bitrate.HasValue || string.IsNullOrEmpty(bitrate.Name))
      {
        continue;
      }

      var oldGenre = context.Genres.FirstOrDefault(g => g.GenreId == bitrate.GenreId);
      var foundOrCreatedGenre = await TryGetOrCreate(bitrate.Name);
      if (foundOrCreatedGenre != null)
      {
        var genre = context.Genres.First(rec => rec.GenreId == foundOrCreatedGenre.GenreId);
        genre.Bitrate = bitrate.Bitrate;
      }

      TryRemove(oldGenre?.Name).Wait();
      context.SaveChanges();
    }
  }

  public bool Exists(Guid genreId)
  {
    using var context = contextFactory();
    return context.Genres.Any(genre => genre.GenreId == genreId);
  }

  public Genres List(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = 0, int take = 100)
  {
    using var context = contextFactory();
    var query = context.Genres
     .Where(g => string.IsNullOrEmpty(filter) || EF.Functions.ILike(g.Name, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(genre => genre.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))));
    }

    var count = query.Count();
    var genres = query
      .OrderBy(g => g.Name)
      .Skip(skip)
      .Take(take)
      .Select(g => mapper.Map<Genre>(g))
      .ToList();

    return new Genres
    {
      TotalCount = count,
      Items = genres
    };
  }

  public Genres ListCommon(IEnumerable<Guid> clientGroups)
  {
    using var context = contextFactory();
    var query = context.Genres
      .Where(genre => genre.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))))
      .OrderByDescending(genre => genre.Records.LongCount());

    var count = query.Count();
    var genres = query
      .OrderBy(g => g.Name)
      .Skip(0)
      .Take(15)
      .Select(g => mapper.Map<Genre>(g))
      .ToList();

    return new Genres
    {
      TotalCount = count,
      Items = genres
    };
  }

  public async Task TryRemove(string? genreName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(genreName))
    {
      return;
    }

    if (!context.Records.Any(rec => rec.Genre != null && rec.Genre.Name == genreName && rec.Genre.Bitrate == null))
    {
      context.Genres.RemoveRange(context.Genres.Where(rec => rec.Name == genreName && rec.Bitrate == null));
    }
    
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<Genre?> TryGetOrCreate(string? genreName)
  {
    using var context = contextFactory();
    if (string.IsNullOrEmpty(genreName))
    {
      return null;
    }

    var genre = context.Genres.FirstOrDefault(g => g.Name == genreName);
    genre ??= new DBContext.Models.Genre
    {
      Name = genreName
    };

    if (genre.GenreId == Guid.Empty)
    {
      context.Genres.Add(genre);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
    
    return mapper.Map<Genre?>(genre);
  }
}
