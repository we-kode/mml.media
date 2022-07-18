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

public class SqlRecordsRepository : IRecordsRepository
{
  private readonly Func<ApplicationDBContext> _contextFactory;
  private readonly IMapper mapper;

  public SqlRecordsRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper)
  {
    _contextFactory = contextFactory;
    this.mapper = mapper;
  }

  public bool IsIndexed(string checksum)
  {
    using var context = _contextFactory();
    return context.Records.Any(record => record.Checksum == checksum);
  }

  public Records List(string filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Groups)
      .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike((rec.Title ?? "").ToLower(), $"%{filter.ToLower()}%"));

    if (filterByGroups)
    {
      query = query.Where(rec => !filterByGroups || rec.Groups.Any(g => groups.Contains(g.GroupId)));
    }

    if (tagFilter.StartDate.HasValue && tagFilter.EndDate.HasValue && tagFilter.EndDate >= tagFilter.StartDate)
    {
      query = query.Where(rec => tagFilter.StartDate.Value.ToUniversalTime().Date <= rec.Date.ToUniversalTime().Date && rec.Date.ToUniversalTime().Date <= tagFilter.EndDate.Value.ToUniversalTime().Date);
    }

    if (tagFilter.Artists.Count > 0)
    {
      query = query.Where(rec => (rec.ArtistId.HasValue && tagFilter.Artists.Contains(rec.ArtistId.Value)));
    }

    if (tagFilter.Genres.Count > 0)
    {
      query = query.Where(rec => (rec.GenreId.HasValue && tagFilter.Genres.Contains(rec.GenreId.Value)));
    }

    if (tagFilter.Albums.Count == 0)
    {
      query = query.Where(rec => (rec.AlbumId.HasValue && tagFilter.Albums.Contains(rec.AlbumId.Value)));
    }

    query = query
      .OrderByDescending(rec => rec.Date.Date)
      .ThenBy(rec => rec.Date.ToLocalTime());

    var count = query.Count();
    var records = query
      .Select(rec => MapModel(rec))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Records
    {
      TotalCount = count,
      Items = records
    };
  }

  private static Record MapModel(DBContext.Models.Records record)
  {
    return new Record(
      record.RecordId,
      record.Title,
      record.Artist?.Name,
      record.Date,
      record.Duration,
      record.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault)).ToArray()
      );
  }

  public async Task SaveMetaData(RecordMetaData metaData)
  {
    using var scope = new TransactionScope();
    using var context = _contextFactory();

    var record = context.Records.FirstOrDefault(record => record.Checksum == metaData.Checksum);
    if (record != null)
    {
      scope.Complete();
      return;
    }

    record = new DBContext.Models.Records
    {
      Checksum = metaData.Checksum,
      FilePath = metaData.PhysicalFilePath,
      Date = metaData.Date,
      Duration = metaData.Duration,
      MimeType = metaData.MimeType,
      TrackNumber = metaData.TrackNumber,
      Title = metaData.Title ?? metaData.OriginalFileName
    };

    // add groups
    var groups = context.Groups.Where(g => g.IsDefault);
    foreach (var group in groups)
    {
      record.Groups.Add(group);
    }

    if (!string.IsNullOrEmpty(metaData.Artist))
    {
      // add artist
      var artist = context.Artists.FirstOrDefault(art => art.Name == metaData.Artist);
      if (artist == null)
      {
        artist = new DBContext.Models.Artists
        {
          Name = metaData.Artist
        };
      }
      record.Artist = artist;
    }

    if (!string.IsNullOrEmpty(metaData.Genre))
    {
      // add genre
      var genre = context.Genres.FirstOrDefault(g => g.Name == metaData.Genre);
      if (genre == null)
      {
        genre = new DBContext.Models.Genres
        {
          Name = metaData.Genre
        };
      }
      record.Genre = genre;
    }

    if (!string.IsNullOrEmpty(metaData.Album))
    {
      // add album
      var album = context.Albums.FirstOrDefault(a => a.AlbumName == metaData.Album);
      if (album == null)
      {
        album = new DBContext.Models.Albums
        {
          AlbumName = metaData.Album
        };
      }
      record.Album = album;
    }

    context.Records.Add(record);
    await context.SaveChangesAsync().ConfigureAwait(false);
    scope.Complete();
    return;
  }

  public Albums ListAlbums(int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Albums
     .OrderBy(album => album.AlbumName);

    var count = query.Count();
    var albums = query
      .Select(album => mapper.Map<Album>(album))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Albums
    {
      TotalCount = count,
      Items = albums
    };
  }

  public Artists ListArtists(int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Artists
     .OrderBy(artist => artist.Name);

    var count = query.Count();
    var artists = query
      .Select(artist => mapper.Map<Artist>(artist))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Artists
    {
      TotalCount = count,
      Items = artists
    };
  }

  public Genres ListGenres(int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Genres
     .OrderBy(g => g.Name);

    var count = query.Count();
    var genres = query
      .Select(g => mapper.Map<Genre>(g))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Genres
    {
      TotalCount = count,
      Items = genres
    };
  }
}
