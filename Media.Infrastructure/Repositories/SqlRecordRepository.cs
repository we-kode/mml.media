using AutoMapper;
using Media.Application.Contracts.Repositories;
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

namespace Media.Infrastructure.Repositories;

public class SqlRecordsRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper, IGroupRepository groupRepository) : IRecordRepository
{
  public bool IsIndexed(string checksum)
  {
    using var context = contextFactory();
    return context.Records.Any(record => record.Checksum == checksum);
  }

  public Records List(string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    using var context = contextFactory();
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
    using var context = contextFactory();
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

  private static IQueryable<DBContext.Models.Record> CreateFilterQuery(ApplicationDBContext context, string? filter, TagFilter tagFilter, bool filterByGroups, bool filterByDate, IList<Guid> groups)
  {
    var query = context.Records
     .Include(rec => rec.Artist)
     .Include(rec => rec.Album)
     .Include(rec => rec.Genre)
     .Include(rec => rec.Language)
     .Include(rec => rec.Groups)
     .Where(rec => string.IsNullOrEmpty(filter) || EF.Functions.ILike(rec.Title, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(rec => rec.Groups.Any(g => groups.Contains(g.GroupId)));
    }

    if (tagFilter.Groups.Count > 0)
    {
      query = query.Where(rec => rec.Groups.Any(g => tagFilter.Groups.Contains(g.GroupId)));
    }

    if (tagFilter.Artists.Count > 0)
    {
      query = query.Where(rec => rec.ArtistId.HasValue && tagFilter.Artists.Contains(rec.ArtistId.Value));
    }

    if (tagFilter.Genres.Count > 0)
    {
      query = query.Where(rec => rec.GenreId.HasValue && tagFilter.Genres.Contains(rec.GenreId.Value));
    }

    if (tagFilter.Albums.Count > 0)
    {
      query = query.Where(rec => rec.AlbumId.HasValue && tagFilter.Albums.Contains(rec.AlbumId.Value));
    }

    if (tagFilter.Languages.Count > 0)
    {
      query = query.Where(rec => rec.LanguageId.HasValue && tagFilter.Languages.Contains(rec.LanguageId.Value));
    }

    if (filterByDate)
    {
      query = query.Where(rec => tagFilter.StartDate!.Value.ToUniversalTime().Date <= rec.Date.ToUniversalTime().Date && rec.Date.ToUniversalTime().Date <= tagFilter.EndDate!.Value.ToUniversalTime().Date);
    }

    return query;
  }

  public Record? Next(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat, bool shuffle)
  {
    using var context = contextFactory();
    var query = Filter(context, filter, tagFilter, filterByGroups, clientGroups.ToList());
    return DetermineRecord(query, id, repeat, shuffle);
  }

  public Record? Previous(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat)
  {
    using var context = contextFactory();
    var query = Filter(context, filter, tagFilter, filterByGroups, clientGroups.ToList());
    return DetermineRecord(query, id, repeat, reverse: true);
  }

  private static Record? DetermineRecord(IQueryable<DBContext.Models.SeedRecord> query, Guid actualId, bool repeat, bool shuffle = false, bool reverse = false)
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
    // Return null if no repeat is set, else return the first element if we want to get the next value, else get the last element.
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

  private static IQueryable<DBContext.Models.SeedRecord> Filter(ApplicationDBContext context, string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups)
  {
    var filterQuery = string.IsNullOrEmpty(filter) ? "%%" : $"%{filter}%";
    var filterGroupsQuery = filterByGroups ? $"AND t.group_id IN ({string.Join(',', groups.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterDateQuery = tagFilter.StartDate.HasValue && tagFilter.EndDate.HasValue && tagFilter.EndDate >= tagFilter.StartDate ? $"AND (('{tagFilter.StartDate.Value.ToUniversalTime().Date:O}' <= date_trunc('day', rec.date::timestamptz, 'UTC')) AND date_trunc('day', rec.date::timestamptz, 'UTC') <= '{tagFilter.EndDate.Value.ToUniversalTime().Date:O}')" : string.Empty;
    var filterArtistsQuery = tagFilter.Artists.Count > 0 ? $"AND rec.artist_id IN ({string.Join(',', tagFilter.Artists.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterGenreQuery = tagFilter.Genres.Count > 0 ? $"AND rec.genre_id IN ({string.Join(',', tagFilter.Genres.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterAlbumQuery = tagFilter.Albums.Count > 0 ? $"AND rec.album_id IN ({string.Join(',', tagFilter.Albums.Select(id => string.Format("'{0}'", id)))})" : string.Empty;
    var filterLanguageQuery = tagFilter.Languages.Count > 0 ? $"AND rec.language_id IN ({string.Join(',', tagFilter.Languages.Select(id => string.Format("'{0}'", id)))})" : string.Empty;

    var selectQuery = @"SELECT 
                  rec.*, a.name as artist_name, t.group_id, al.album_name as album_name, g.name as genre_name, l.name as language_name,
                  LEAD(rec.record_id, 1) OVER(ORDER BY Cast(rec.date as Date) DESC, rec.date) as next_id, 
                  LAG(rec.record_id, 1) OVER(ORDER BY Cast(rec.date as Date) DESC, rec.date) as previous_id
                  FROM public.records AS rec
                  LEFT JOIN public.artists AS a ON rec.artist_id = a.artist_id
                  LEFT JOIN public.albums AS al ON rec.album_id = al.album_id
                  LEFT JOIN public.genres AS g ON rec.genre_id = g.genre_id
                  LEFT JOIN public.languages AS l ON rec.language_id = l.language_id
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

  private static Record? MapModel(DBContext.Models.SeedRecord? record)
  {
    if (record == null)
    {
      return null;
    }

    return new Record(
      record.RecordId,
      record.Title,
      record.TrackNumber,
      record.ArtistName,
      record.Date,
      record.Duration,
      record.Bitrate ?? 0,
      null!,
      record.AlbumName ?? "",
      record.GenreName ?? "",
      record.LanguageName ?? "",
      checksum: record.Checksum,
      record.Cover ?? string.Empty);
  }

  public void SaveMetaData(RecordMetaData metaData, List<Guid> groups)
  {
    using var context = contextFactory();

    var record = context.Records.FirstOrDefault(record => record.Checksum == metaData.Checksum);
    if (record != null)
    {
      return;
    }

    record = new DBContext.Models.Record
    {
      Checksum = metaData.Checksum,
      FilePath = metaData.PhysicalFilePath,
      Date = metaData.Date,
      Duration = metaData.Duration,
      MimeType = metaData.MimeType,
      TrackNumber = metaData.TrackNumber,
      Title = metaData.Title ?? metaData.OriginalFileName,
      Bitrate = metaData.Bitrate,
      Cover = metaData.Cover,
    };

    // add groups
    var availableGroups = context.Groups.Where(g => groups.Contains(g.GroupId));
    foreach (var group in availableGroups)
    {
      record.Groups.Add(group);
    }

    record.ArtistId = metaData.ArtistId;
    record.GenreId = metaData.GenreId;
    record.AlbumId = metaData.AlbumId;
    record.LanguageId = metaData.LanguageId;

    context.Records.Add(record);
    context.SaveChanges();
  }

  public async Task RemoveRecord(Guid recordId) {
    using var context = contextFactory();
    var record = context.Records.FirstOrDefault(rec => rec.RecordId == recordId);
    if (record == null)
    {
      return;
    }

    context.Records.Remove(record);
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
    using var context = contextFactory();
    var record = context.Records.FirstOrDefault(rec => rec.RecordId == id);
    if (record == null)
    {
      return false;
    }
    return File.Exists(Path.Combine(record.FilePath, record.Checksum));
  }

  public Record GetRecord(Guid id)
  {
    using var context = contextFactory();
    var record = context.Records
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Artist)
      .Include(rec => rec.Groups)
      .Include(rec => rec.Language)
      .First(rec => rec.RecordId == id);

    return MapModel(record);
  }

  public Record? TryGetRecord(Guid id)
  {
    using var context = contextFactory();
    var record = context.Records
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Artist)
      .Include(rec => rec.Groups)
      .Include(rec => rec.Language)
      .FirstOrDefault(rec => rec.RecordId == id);

    return record == null ? null : MapModel(record);
  }

  private static Record MapModel(DBContext.Models.Record record)
  {
    return new Record(
      record.RecordId,
      record.Title,
      record.TrackNumber,
      record.Artist?.Name,
      record.Date,
      record.Duration,
      record.Bitrate ?? 0,
      record.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault)).ToArray(),
      record.Album?.AlbumName ?? string.Empty,
      record.Genre?.Name ?? string.Empty,
      record.Language?.Name ?? string.Empty,
      record.Checksum,
      record.Cover ?? string.Empty,
      record.Locked);
  }

  public async Task Update(Record record, (Guid? artistId, Guid? albumId, Guid? genreId, Guid? languageId) references)
  {
    using var context = contextFactory();

    var recordToUpdated = context.Records
      .Include(rec => rec.Artist)
      .Include(rec => rec.Album)
      .Include(rec => rec.Genre)
      .Include(rec => rec.Groups)
      .Include(rec => rec.Language)
      .FirstOrDefault(rec => rec.RecordId == record.RecordId);
    if (recordToUpdated == null)
    {
      return;
    }

    // Update title
    recordToUpdated.Title = record.Title;

    recordToUpdated.ArtistId = references.artistId;
    recordToUpdated.AlbumId = references.albumId;
    recordToUpdated.GenreId = references.genreId;
    recordToUpdated.LanguageId = references.languageId;

    // update groups
    var addedGroups = record.Groups
       .Where(g => groupRepository.GroupExists(g.Id).GetAwaiter().GetResult())
       .Where(g => !recordToUpdated.Groups.Any(rg => rg.GroupId == g.Id))
       .Select(g => new DBContext.Models.Group
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

    recordToUpdated.Cover = record.Cover.Length > 0 ? Convert.ToBase64String(record.Cover) : null;
    recordToUpdated.Locked = record.Locked;

    await context.SaveChangesAsync().ConfigureAwait(false);
  }


  public RecordStream StreamRecord(Guid id)
  {
    using var context = contextFactory();
    var record = context.Records.First(rec => rec.RecordId == id);
    var stream = File.OpenRead(Path.Combine(record.FilePath, record.Checksum));
    return new RecordStream(record.MimeType, stream);
  }

  public bool IsInGroup(Guid id, IEnumerable<Guid> clientGroups)
  {
    using var context = contextFactory();
    var record = context.Records
      .Include(r => r.Groups)
      .First(rec => rec.RecordId == id);
    return record.Groups.Any(g => clientGroups.Contains(g.GroupId));
  }

  public string GetFilePath(Guid id)
  {
    using var context = contextFactory();
    var record = context.Records.First(rec => rec.RecordId == id);
    return Path.Combine(record.FilePath, record.Checksum);
  }

  public List<Record> GetRecords(List<string> checksums, IList<Guid> clientGroups)
  {
    using var context = contextFactory();
    return [.. context.Records
      .Include(rec => rec.Groups)
      .Where(rec => checksums.Contains(rec.Checksum))
      .Where(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId)))
      .Select(rec => MapModel(rec))];
  }

  public void Assign(List<Guid> items, List<Guid> initGroups, List<Guid> groups)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = contextFactory();
    var rAssign = context.Records
      .Include(app => app.Groups)
      .Where(rec => items.Contains(rec.RecordId)).ToList();
    var gAssign = context.Groups
     .Where(g => groups.Contains(g.GroupId));
    foreach (var record in rAssign)
    {
      record.Groups = record.Groups = record.Groups.Where(rg => initGroups.Contains(rg.GroupId) && !groups.Contains(rg.GroupId)).Union(gAssign).ToList();
    }
    context.SaveChanges();
    scope.Complete();
  }

  public void AssignFolder(IEnumerable<RecordFolder> items, List<Guid> initGroups, List<Guid> groups)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = contextFactory();
    var gAssign = context.Groups.Where(g => groups.Contains(g.GroupId));
    foreach (var folder in items)
    {
      var dateRange = folder.ToDateRange();
      var records = context.Records.Include(app => app.Groups).Where(rec => rec.Date.Date >= dateRange.Item1 && rec.Date.Date <= dateRange.Item2).ToList();

      foreach (var record in records)
      {
        record.Groups = record.Groups = record.Groups.Where(rg => initGroups.Contains(rg.GroupId) && !groups.Contains(rg.GroupId)).Union(gAssign).ToList();
      }
    }
    context.SaveChanges();
    scope.Complete();
  }

  public void Lock(List<Guid> items)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = contextFactory();
    var rAssign = context.Records
      .Where(rec => items.Contains(rec.RecordId)).ToList();
    foreach (var record in rAssign)
    {
      record.Locked = !record.Locked;
    }
    context.SaveChanges();
    scope.Complete();
  }

  public void LockFolder(IEnumerable<RecordFolder> items)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    using var context = contextFactory();
    foreach (var folder in items)
    {
      var dateRange = folder.ToDateRange();
      var records = context.Records.Where(rec => rec.Date.Date >= dateRange.Item1 && rec.Date.Date <= dateRange.Item2).ToList();

      foreach (var record in records)
      {
        record.Locked = !record.Locked;
      }
    }
    context.SaveChanges();
    scope.Complete();
  }

  public Groups GetAssignedGroups(List<Guid> items)
  {
    using var context = contextFactory();
    var groups = context.Groups
      .Where(g => g.Records.Any(c => items.Contains(c.RecordId)));

    var count = groups.Count();

    return new Groups
    {
      TotalCount = count,
      Items = [..mapper.ProjectTo<Group>(groups)],
    };
  }

  public Groups GetAssignedFolderGroups(IEnumerable<RecordFolder> folders)
  {
    using var context = contextFactory();
    var recs = new List<Guid>();
    foreach (var folder in folders)
    {
      var dateRange = folder.ToDateRange();
      recs.AddRange(context.Records.Where(rec => rec.Date.Date >= dateRange.Item1 && rec.Date.Date <= dateRange.Item2).Select(r => r.RecordId));
    }

    var groups = context.Groups
      .Where(g => g.Records.Any(c => recs.Contains(c.RecordId)));

    var count = groups.Count();

    return new Groups
    {
      TotalCount = count,
      Items = [..mapper.ProjectTo<Group>(groups)],
    };
  }

  public List<Guid> GetRecords(RecordFolder folder)
  {
    using var context = contextFactory();
    var dateRange = folder.ToDateRange();
    return context.Records.Where(rec => rec.Date.Date >= dateRange.Item1 && rec.Date.Date <= dateRange.Item2).Select(rec => rec.RecordId).ToList();
  }
}
