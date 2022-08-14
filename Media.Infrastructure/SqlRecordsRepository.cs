using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Media.Infrastructure;

public class SqlRecordsRepository : IRecordsRepository
{
  private readonly Func<ApplicationDBContext> _contextFactory;
  private readonly IMapper mapper;
  private readonly IGroupRepository _groupRepository;

  public SqlRecordsRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper, IGroupRepository groupRepository)
  {
    _contextFactory = contextFactory;
    this.mapper = mapper;
    _groupRepository = groupRepository;
  }

  public bool IsIndexed(string checksum)
  {
    using var context = _contextFactory();
    return context.Records.Any(record => record.Checksum == checksum);
  }

  public Records List(string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Records
         .Include(rec => rec.Artist)
         .Include(rec => rec.Groups)
         .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike(rec.Title, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(rec => rec.Groups.Any(g => groups.Contains(g.GroupId)));
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

    if (tagFilter.Albums.Count > 0)
    {
      query = query.Where(rec => (rec.AlbumId.HasValue && tagFilter.Albums.Contains(rec.AlbumId.Value)));
    }

    query = query
      .OrderByDescending(rec => rec.Date.Date)
      .ThenBy(rec => rec.Date);


    var count = query.Count();
    var records = query
      .Skip(skip)
      .Take(take)
      .Select(rec => MapModel(rec))
      .ToList();

    return new Records
    {
      TotalCount = count,
      Items = records
    };
  }

  public Record? Next(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat, bool shuffle)
  {
    using var context = _contextFactory();
    var query = Filter(context, filter, tagFilter, filterByGroups, clientGroups.ToList());
    return DetermineRecord(query, id, repeat, shuffle);
  }

  public Record? Previous(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat)
  {
    using var context = _contextFactory();
    var query = Filter(context, filter, tagFilter, filterByGroups, clientGroups.ToList());
    return DetermineRecord(query, id, repeat, reverse: true);
  }

  private Record? DetermineRecord(IQueryable<DBContext.Models.SeedRecords> query, Guid actualId, bool repeat, bool shuffle = false, bool reverse = false)
  {
    // If no element in result return null.
    var count = query.Count();
    if (count == 0)
    {
      return null;
    }

    // If shuffle, than the result is randomized take one random value from list
    if (shuffle)
    {
      var randomIndex = new Random().Next(count - 1);
      return MapModel(query.Skip(randomIndex).First());
    }

    // If actual record is not in result, than filter has changed, start from beginning.
    if (query.FirstOrDefault(rec => rec.RecordId == actualId) == null)
    {
      return MapModel(query.First());
    }

    // Skip all elements until id reached. Take th next value too. If previous is expexted the query will be reversed.
    // If only the actualId is in result, the end or beginning has been reached.
    // Return null if no repeat is set, else return the first elemtn if we want to get tjhe next value, else get the last element.
    var actual = query.First(rec => rec.RecordId == actualId);
    if (!reverse)
    {
      var nextId = actual.NextId;
      if (!nextId.HasValue && !repeat)
      {
        return null;
      }

      if (!nextId.HasValue && repeat)
      {
        return MapModel(query.First());
      }

      return MapModel(query.First(rec => rec.RecordId == nextId));
    }

    // return previous value
    if (reverse)
    {
      var previousId = actual.PreviousId;
      if (!previousId.HasValue && !repeat)
      {
        return null;
      }

      if (!previousId.HasValue && repeat)
      {
        return MapModel(query.Last());
      }

      return MapModel(query.First(rec => rec.RecordId == previousId));
    }

    return null;
  }

  private IQueryable<DBContext.Models.SeedRecords> Filter(ApplicationDBContext context, string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups)
  {
    var query = context.SeedRecords
     .Include(rec => rec.Artist)
     .Join(context.Records.Include(r => r.Groups).Where(rec => !filterByGroups || rec.Groups.Any(g => groups.Contains(g.GroupId))),
      seed => seed.RecordId,
      rec => rec.RecordId,
      (seed, record) => new DBContext.Models.SeedRecords
      {
        Album = seed.Album,
        AlbumId = seed.AlbumId,
        Artist = seed.Artist,
        ArtistId = seed.ArtistId,
        Date = seed.Date,
        Duration = seed.Duration,
        Genre = seed.Genre,
        GenreId = seed.GenreId,
        RecordId = seed.RecordId,
        Title = seed.Title,
        NextId = seed.NextId,
        PreviousId = seed.PreviousId
      })
     .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike(rec.Title, $"%{filter}%"));

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

    if (tagFilter.Albums.Count > 0)
    {
      query = query.Where(rec => (rec.AlbumId.HasValue && tagFilter.Albums.Contains(rec.AlbumId.Value)));
    }

    return query;
  }

  private static Record MapModel(DBContext.Models.SeedRecords record)
  {
    return new Record(
      record.RecordId,
      record.Title,
      record.Artist?.Name,
      record.Date,
      record.Duration,
      null!,
      record.Album?.AlbumName ?? string.Empty,
      record.Genre?.Name ?? string.Empty
      );
  }

  public void SaveMetaData(RecordMetaData metaData)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
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

    record.Artist = TryGetArtist(context, metaData.Artist);
    record.Genre = TryGetGenre(context, metaData.Genre);
    record.Album = TryGetAlbum(context, metaData.Album);

    context.Records.Add(record);
    context.SaveChanges();
    scope.Complete();
  }

  public Albums ListAlbums(string? filter, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Albums
     .Where(al => string.IsNullOrEmpty(filter) || EF.Functions.ILike(al.AlbumName, $"%{filter}%"))
     .OrderBy(album => album.AlbumName);

    var count = query.Count();
    var albums = query
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

  public Artists ListArtists(string? filter, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Artists
     .Where(ar => string.IsNullOrEmpty(filter) || EF.Functions.ILike(ar.Name, $"%{filter}%"))
     .OrderBy(artist => artist.Name);

    var count = query.Count();
    var artists = query
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

  public Genres ListGenres(string? filter, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
    var query = context.Genres
     .Where(g => string.IsNullOrEmpty(filter) || EF.Functions.ILike(g.Name, $"%{filter}%"))
     .OrderBy(g => g.Name);

    var count = query.Count();
    var genres = query
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

  public async Task DeleteRecord(Guid id)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();

    var record = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .FirstOrDefault(record => record.RecordId == id);
    if (record == null)
    {
      scope.Complete();
      return;
    }

    // Delete entry from db
    context.Records.Remove(record);
    await context.SaveChangesAsync().ConfigureAwait(false);

    // Remove artist if no record connected
    TryRemoveArtist(context, record.Artist);

    // remove album if nor record connected
    TryRemoveAlbum(context, record.Album);

    // remove genre if no record connected
    TryRemoveGenre(context, record.Genre);

    await context.SaveChangesAsync().ConfigureAwait(false);

    // delete file from folder
    var filePath = $"{record.FilePath}{record.Checksum}";
    if (File.Exists(filePath))
    {
      File.Delete(filePath);
    }
    scope.Complete();
  }

  public bool Exists(Guid id)
  {
    using var context = _contextFactory();
    var record = context.Records.FirstOrDefault(rec => rec.RecordId == id);
    if (record == null)
    {
      return false;
    }
    return File.Exists(Path.Combine(record.FilePath, record.Checksum));
  }

  public Record GetRecord(Guid id)
  {
    using var context = _contextFactory();
    var record = context.Records
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Artist)
      .Include(rec => rec.Groups)
      .First(rec => rec.RecordId == id);

    return MapModel(record);
  }

  private static Record MapModel(DBContext.Models.Records record)
  {
    return new Record(
      record.RecordId,
      record.Title,
      record.Artist?.Name,
      record.Date,
      record.Duration,
      record.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault)).ToArray(),
      record.Album?.AlbumName ?? string.Empty,
      record.Genre?.Name ?? string.Empty
      );
  }

  public async Task Update(Record record)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();

    var recordToUpdated = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Groups)
      .FirstOrDefault(rec => rec.RecordId == record.RecordId);
    if (recordToUpdated == null)
    {
      scope.Complete();
      return;
    }

    // Update title
    recordToUpdated.Title = record.Title;

    // update artist
    var oldArtist = recordToUpdated.Artist;
    if (recordToUpdated.Artist?.Name != record.Artist)
    {
      recordToUpdated.Artist = TryGetArtist(context, record.Artist);
    }

    // if to != null && new null rmove
    // if new != null try remove old try get new and assiggn it

    // update album
    var oldAlbum = recordToUpdated.Album;
    if (recordToUpdated.Album?.AlbumName != record.Album)
    {
      recordToUpdated.Album = TryGetAlbum(context, record.Album);
    }

    // update genre
    var oldGenre = recordToUpdated.Genre;
    if (recordToUpdated.Genre?.Name != record.Genre)
    {
      recordToUpdated.Genre = TryGetGenre(context, record.Genre);
    }

    // update groups
    var addedGroups = record.Groups
       .Where(g => _groupRepository.GroupExists(g.Id).GetAwaiter().GetResult())
       .Where(g => !recordToUpdated.Groups.Any(rg => rg.GroupId == g.Id))
       .Select(g => new DBContext.Models.Groups
       {
         GroupId = g.Id,
         Name = g.Name,
         IsDefault = g.IsDefault
       })
       .ToArray();

    var deletedGroups = recordToUpdated.Groups
      .Where(g => !record.Groups.Any(rg => rg.Id == g.GroupId))
      .ToArray();

    foreach (var addedGroup in addedGroups)
    {
      recordToUpdated.Groups.Add(addedGroup);
    }

    foreach (var deletedGroup in deletedGroups)
    {
      recordToUpdated.Groups.Remove(deletedGroup);
    }

    await context.SaveChangesAsync().ConfigureAwait(false);

    // try remove old data
    TryRemoveArtist(context, oldArtist);
    TryRemoveGenre(context, oldGenre);
    TryRemoveAlbum(context, oldAlbum);

    await context.SaveChangesAsync().ConfigureAwait(false);
    scope.Complete();
  }

  private static void TryRemoveArtist(ApplicationDBContext context, DBContext.Models.Artists? artist)
  {
    if (artist == null)
    {
      return;
    }

    if (!context.Records.Any(rec => rec.ArtistId == artist.ArtistId))
    {
      context.Artists.Remove(artist);
    }
  }

  private static DBContext.Models.Artists? TryGetArtist(ApplicationDBContext context, string? artistName)
  {
    if (string.IsNullOrEmpty(artistName))
    {
      return null;
    }

    var artist = context.Artists.FirstOrDefault(art => art.Name == artistName);
    if (artist == null)
    {
      artist = new DBContext.Models.Artists
      {
        Name = artistName
      };
    }

    return artist;
  }

  private static void TryRemoveAlbum(ApplicationDBContext context, DBContext.Models.Albums? album)
  {
    if (album == null)
    {
      return;
    }

    if (!context.Records.Any(rec => rec.AlbumId == album.AlbumId))
    {
      context.Albums.Remove(album);
    }
  }

  private static DBContext.Models.Albums? TryGetAlbum(ApplicationDBContext context, string? albumName)
  {
    if (string.IsNullOrEmpty(albumName))
    {
      return null;
    }

    var album = context.Albums.FirstOrDefault(a => a.AlbumName == albumName);
    if (album == null)
    {
      album = new DBContext.Models.Albums
      {
        AlbumName = albumName
      };
    }

    return album;
  }

  private static void TryRemoveGenre(ApplicationDBContext context, DBContext.Models.Genres? genre)
  {
    if (genre == null)
    {
      return;
    }

    if (!context.Records.Any(rec => rec.GenreId == genre.GenreId))
    {
      context.Genres.Remove(genre);
    }
  }

  private static DBContext.Models.Genres? TryGetGenre(ApplicationDBContext context, string? genreName)
  {
    if (string.IsNullOrEmpty(genreName))
    {
      return null;
    }

    var genre = context.Genres.FirstOrDefault(g => g.Name == genreName);
    if (genre == null)
    {
      genre = new DBContext.Models.Genres
      {
        Name = genreName
      };
    }

    return genre;
  }

  public RecordStream StreamRecord(Guid id)
  {
    using var context = _contextFactory();
    var record = context.Records.First(rec => rec.RecordId == id);
    var stream = File.OpenRead(Path.Combine(record.FilePath, record.Checksum));
    return new RecordStream(record.MimeType, stream);
  }

  public bool IsInGroup(Guid id, IEnumerable<Guid> clientGroups)
  {
    using var context = _contextFactory();
    var record = context.Records
      .Include(r => r.Groups)
      .First(rec => rec.RecordId == id);
    return record.Groups.Any(g => clientGroups.Contains(g.GroupId));
  }
}
