using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

  public Application.Models.Records List(string filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Groups)
      .Where(rec => !tagFilter.StartDate.HasValue || !tagFilter.EndDate.HasValue || tagFilter.EndDate >= tagFilter.StartDate && tagFilter.StartDate.Value.ToUniversalTime().Date <= rec.Date.ToUniversalTime().Date && rec.Date.ToUniversalTime().Date <= tagFilter.EndDate.Value.ToUniversalTime().Date)
      .Where(rec => tagFilter.Artists.Count == 0 || (rec.ArtistId.HasValue && tagFilter.Artists.Contains(rec.ArtistId.Value)))
      .Where(rec => tagFilter.Genres.Count == 0 || (rec.GenreId.HasValue && tagFilter.Artists.Contains(rec.GenreId.Value)))
      .Where(rec => tagFilter.Albums.Count == 0 || (rec.AlbumId.HasValue && tagFilter.Artists.Contains(rec.AlbumId.Value)))
      .Where(rec => !filterByGroups || rec.Groups.Any(g => groups.Contains(g.GroupId)))
      .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike((rec.Title ?? "").ToLower(), $"%{filter.ToLower()}%"))
      .OrderByDescending(rec => rec.Date.Date)
      .ThenBy(rec => rec.Date.ToLocalTime());

    var count = query.Count();
    var records = query
      .Select(rec => MapModel(rec))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Application.Models.Records
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
      record.Genre?.Name,
      record.Album?.AlbumName,
      record.Date,
      record.Duration,
      record.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault)).ToArray()
      );
  }

  public void SaveMetaData(RecordMetaData metaData)
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

    if (metaData.DefaultGroupId.HasValue)
    {
      // add groups
      var group = context.Groups.FirstOrDefault(g => g.GroupId == metaData.DefaultGroupId.Value);
      if (group != null)
      {
        record.Groups.Add(group);
      }
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
    context.SaveChanges();
    scope.Complete();
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
    var artists = query
      .Select(g => mapper.Map<Genre>(g))
      .Skip(skip)
      .Take(take)
      .ToList();

    return new Genres
    {
      TotalCount = count,
      Items = artists
    };
  }
}
