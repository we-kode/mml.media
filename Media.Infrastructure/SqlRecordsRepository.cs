using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    var query = CreateFilterQuery(context, filter, tagFilter, filterByGroups, tagFilter.StartDate.HasValue && tagFilter.EndDate.HasValue && tagFilter.EndDate >= tagFilter.StartDate, groups);

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

  public RecordFolders ListFolder(string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip, int take)
  {
    using var context = _contextFactory();
    var dateFilterSet = tagFilter.StartDate.HasValue && tagFilter.EndDate.HasValue && tagFilter.EndDate >= tagFilter.StartDate;
    var query = CreateFilterQuery(context, filter, tagFilter, filterByGroups, dateFilterSet, groups);

    var isYearFilter = !dateFilterSet;
    var isMonthFilter = dateFilterSet && tagFilter.StartDate!.Value.Year == tagFilter.EndDate!.Value.Year && tagFilter.StartDate!.Value.Month != tagFilter.EndDate!.Value.Month;
    var isDayFilter = dateFilterSet && tagFilter.StartDate!.Value.Month == tagFilter.EndDate!.Value.Month;

    var groupedQuery = query
      .GroupBy(rec => isYearFilter ? rec.Date.Year : isMonthFilter ? rec.Date.Month : rec.Date.Day)
      .OrderByDescending(rec => rec.Key);

    var count = groupedQuery.Count();
    var folders = groupedQuery
      .Skip(skip)
      .Take(take)
      .Select(rec => new RecordFolder
      {
        Year = isYearFilter ? rec.Key : tagFilter.StartDate!.Value.Year,
        Month = isMonthFilter ? rec.Key : isDayFilter ? tagFilter.StartDate!.Value.Month : null,
        Day = isDayFilter ? rec.Key : null,
      })
      .ToList();

    return new RecordFolders
    {
      TotalCount = count,
      Items = folders
    };
  }

  private static IQueryable<DBContext.Models.Records> CreateFilterQuery(ApplicationDBContext context, string? filter, TagFilter tagFilter, bool filterByGroups, bool filterByDate, IList<Guid> groups)
  {
    var query = context.Records
     .Include(rec => rec.Artist)
     .Include(rec => rec.Groups)
     .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike(rec.Title, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(rec => rec.Groups.Any(g => groups.Contains(g.GroupId)));
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

    if (tagFilter.Languages.Count > 0)
    {
      query = query.Where(rec => (rec.LanguageId.HasValue && tagFilter.Languages.Contains(rec.LanguageId.Value)));
    }

    if (filterByDate)
    {
      query = query.Where(rec => tagFilter.StartDate!.Value.ToUniversalTime().Date <= rec.Date.ToUniversalTime().Date && rec.Date.ToUniversalTime().Date <= tagFilter.EndDate!.Value.ToUniversalTime().Date);
    }

    return query;
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

    // If shuffle, then the result is randomized take one random value from list
    if (shuffle)
    {
      var randomIndex = new Random().Next(count - 1);
      return MapModel(query.Skip(randomIndex).FirstOrDefault());
    }

    // If actual record is not in result, then filter has changed, start from beginning.
    if (query.FirstOrDefault(rec => rec.RecordId == actualId) == null)
    {
      return MapModel(query.FirstOrDefault());
    }

    // Skip all elements until id reached. Take the expected value. If previous is expected the query will be reversed.
    // If only the actualId is in result, the end or beginning has been reached.
    // Return null if no repeat is set, else return the first elemtn if we want to get tjhe next value, else get the last element.
    var actual = query.FirstOrDefault(rec => rec.RecordId == actualId);
    if (actual == null)
    {
      return null;
    }

    if (!reverse)
    {
      var nextId = actual.NextId;
      if (!nextId.HasValue && repeat)
      {
        return MapModel(query.FirstOrDefault());
      }

      return MapModel(query.FirstOrDefault(rec => rec.RecordId == nextId));
    }

    // return previous value
    var previousId = actual.PreviousId;
    if (!previousId.HasValue && repeat)
    {
      return MapModel(query.OrderByDescending(r => r.Date.Date).ThenBy(r => r.Date).LastOrDefault());
    }

    return MapModel(query.FirstOrDefault(rec => rec.RecordId == previousId));
  }

  private IQueryable<DBContext.Models.SeedRecords> Filter(ApplicationDBContext context, string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups)
  {
    var filterQuery = string.IsNullOrEmpty(filter) ? "%%" : $"%{filter}%";
    var filterGroupsQuery = filterByGroups ? $"AND t.group_id IN ({string.Join(',', groups.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterDateQuery = tagFilter.StartDate.HasValue && tagFilter.EndDate.HasValue && tagFilter.EndDate >= tagFilter.StartDate ? $"AND (('{tagFilter.StartDate.Value.ToUniversalTime().Date:O}' <= date_trunc('day', rec.date::timestamptz, 'UTC')) AND date_trunc('day', rec.date::timestamptz, 'UTC') <= '{tagFilter.EndDate.Value.ToUniversalTime().Date:O}')" : string.Empty;
    var filterArtistsQuery = tagFilter.Artists.Count > 0 ? $"AND rec.artist_id IN ({string.Join(',', tagFilter.Artists.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterGenreQuery = tagFilter.Genres.Count > 0 ? $"AND rec.genre_id IN ({string.Join(',', tagFilter.Genres.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterAlbumQuery = tagFilter.Albums.Count > 0 ? $"AND rec.album_id IN ({string.Join(',', tagFilter.Albums.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterLanguageQuery = tagFilter.Languages.Count > 0 ? $"AND rec.language_id IN ({string.Join(',', tagFilter.Languages.Select(id => string.Format("'{0}'", id)))})" : string.Empty;

    var selectQuery = @"SELECT 
                  rec.*, a.name as artist_name, t.group_id,
                  LEAD(rec.record_id, 1) OVER(ORDER BY Cast(rec.date as Date) DESC, rec.date) as next_id, 
                  LAG(rec.record_id, 1) OVER(ORDER BY Cast(rec.date as Date) DESC, rec.date) as previous_id
                  FROM public.records AS rec
                  LEFT JOIN public.artists AS a ON rec.artist_id = a.artist_id
                  LEFT JOIN (SELECT g1.groups_group_id, g1.records_record_id, g2.group_id
			                       FROM public.groups_records AS g1
			                       INNER JOIN public.groups AS g2 ON g1.groups_group_id = g2.group_id
                            ) AS t 
                  ON rec.record_id = t.records_record_id
                  WHERE rec.title ILIKE {0}";

    var sb = new StringBuilder();
    sb.AppendLine(selectQuery);
    sb.AppendLine(filterGroupsQuery);
    sb.AppendLine(filterDateQuery);
    sb.AppendLine(filterArtistsQuery);
    sb.AppendLine(filterGenreQuery);
    sb.AppendLine(filterAlbumQuery);
    sb.AppendLine(filterLanguageQuery);
    sb.AppendLine("ORDER BY Cast(rec.date as Date) desc, rec.Date asc");
    var query = context.SeedRecords.FromSqlRaw($"{sb}", filterQuery);

    return query;
  }

  private static Record? MapModel(DBContext.Models.SeedRecords? record)
  {
    if (record == null)
    {
      return null;
    }

    return new Record(
      record.RecordId,
      record.Title,
      record.ArtistName,
      record.Date,
      record.Duration,
      null!,
      checksum: record.Checksum);
  }

  public void SaveMetaData(RecordMetaData metaData, List<Guid> groups)
  {
    try
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
        Title = metaData.Title ?? metaData.OriginalFileName,
      };

      // add groups
      var availableGroups = context.Groups.Where(g => groups.Contains(g.GroupId));
      foreach (var group in availableGroups)
      {
        record.Groups.Add(group);
      }

      record.Artist = TryGetArtist(context, metaData.Artist);
      record.Genre = TryGetGenre(context, metaData.Genre);
      record.Album = TryGetAlbum(context, metaData.Album);
      record.Language = TryGetLanguage(context, metaData.Language);

      context.Records.Add(record);
      context.SaveChanges();
      scope.Complete();
    }
    catch (InvalidOperationException e)
    {
      if (e.InnerException is DbUpdateException)
      {
        SaveMetaData(metaData, groups);
        return;
      }

      throw e;
    }
  }

  public Albums ListAlbums(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
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

  public Artists ListArtists(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
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

  public Genres ListGenres(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = _contextFactory();
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

  public Languages ListLanguages(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = 0, int take = 100)
  {
    using var context = _contextFactory();
    var query = context.Languages
     .Where(al => string.IsNullOrEmpty(filter) || EF.Functions.ILike(al.Name, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(album => album.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))));
    }

    var count = query.Count();
    var langs = query
      .OrderBy(lang => lang.Name)
      .Skip(skip)
      .Take(take)
      .Select(lang => mapper.Map<Language>(lang))
      .ToList();

    return new Languages
    {
      TotalCount = count,
      Items = langs
    };
  }

  public async Task DeleteFolders(IEnumerable<RecordFolder> folders)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();
    foreach (var folder in folders)
    {
      var dateRange = folder.ToDateRange();
      var recordIds = context.Records.Where(rec => rec.Date.Date >= dateRange.Item1 && rec.Date.Date <= dateRange.Item2).Select(rec => rec.RecordId).ToList();
      foreach (var recordId in recordIds)
      {
        await RemoveRecord(context, recordId).ConfigureAwait(false);
      }
    }
    scope.Complete();
  }

  public async Task DeleteRecord(Guid id)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = _contextFactory();
    await RemoveRecord(context, id).ConfigureAwait(false);
    scope.Complete();
  }

  private async Task RemoveRecord(ApplicationDBContext context, Guid id)
  {
    var record = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Language)
      .FirstOrDefault(record => record.RecordId == id);
    if (record == null)
    {
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

    // remove language if no record connected
    TryRemoveLanguage(context, record.Language);

    await context.SaveChangesAsync().ConfigureAwait(false);

    // delete file from folder
    var filePath = $"{record.FilePath}{record.Checksum}";
    if (File.Exists(filePath))
    {
      File.Delete(filePath);
    }
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
      .Include(rec => rec.Language)
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
      record.Genre?.Name ?? string.Empty,
      record.Language?.Name ?? string.Empty,
      record.Checksum);
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
      .Include(rec => rec.Language)
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

    // update language
    var oldLang = recordToUpdated.Language;
    if (recordToUpdated.Language?.Name != record.Language)
    {
      recordToUpdated.Language = TryGetLanguage(context, record.Language);
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
    TryRemoveLanguage(context, oldLang);

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

    if (!context.Records.Any(rec => rec.GenreId == genre.GenreId) && !genre.Bitrate.HasValue)
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
    genre ??= new DBContext.Models.Genres
    {
      Name = genreName
    };

    return genre;
  }

  private static void TryRemoveLanguage(ApplicationDBContext context, DBContext.Models.Languages? lang)
  {
    if (lang == null)
    {
      return;
    }

    if (!context.Records.Any(rec => rec.LanguageId == lang.LanguageId))
    {
      context.Languages.Remove(lang);
    }
  }

  private static DBContext.Models.Languages? TryGetLanguage(ApplicationDBContext context, string? langName)
  {
    if (string.IsNullOrEmpty(langName))
    {
      return null;
    }

    var lang = context.Languages.FirstOrDefault(g => g.Name == langName);
    if (lang == null)
    {
      lang = new DBContext.Models.Languages
      {
        Name = langName
      };
    }

    return lang;
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

  public string GetFilePath(Guid id)
  {
    using var context = _contextFactory();
    var record = context.Records.First(rec => rec.RecordId == id);
    return Path.Combine(record.FilePath, record.Checksum);
  }

  public GenreBitrates Bitrates()
  {
    using var context = _contextFactory();
    var genres = context.Genres
      .Where(genre => genre.Bitrate.HasValue)
      .Select(genre => mapper.Map<GenreBitrate>(genre));
    return new GenreBitrates
    {
      TotalCount = genres.Count(),
      Items = genres.ToList()
    };
  }

  public bool GenreExists(Guid genreId)
  {
    using var context = _contextFactory();
    return context.Genres.Any(genre => genre.GenreId == genreId);
  }

  public void DeleteBitrate(Guid genreId)
  {
    using var context = _contextFactory();
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

  public void UpdateBitrates(List<GenreBitrate> bitrates)
  {
    using var context = _contextFactory();
    foreach (var bitrate in bitrates)
    {
      if (!bitrate.Bitrate.HasValue || string.IsNullOrEmpty(bitrate.Name))
      {
        continue;
      }

      var oldGenre = context.Genres.FirstOrDefault(g => g.GenreId == bitrate.GenreId);
      var genre = TryGetGenre(context, bitrate.Name);
      if (genre != null)
      {
        genre.Bitrate = bitrate.Bitrate;
      }

      TryRemoveGenre(context, oldGenre);
    }

    context.SaveChanges();
  }
}
