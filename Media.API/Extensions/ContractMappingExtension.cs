using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.API.Extensions
{
  public static class ContractMappingExtension
  {
    public static TagFilter Map(this Contracts.TagFilter tagFilter)
    {
      return new TagFilter
      {
        Artists = tagFilter.Artists,
        ArtistNames = tagFilter.ArtistNames,
        Genres = tagFilter.Genres,
        GenreNames = tagFilter.GenreNames,
        Albums = tagFilter.Albums,
        AlbumNames = tagFilter.AlbumNames,
        Languages = tagFilter.Languages,
        Groups = tagFilter.Groups,
        StartDate = tagFilter.StartDate,
        EndDate = tagFilter.EndDate,
        Unassigned = tagFilter.Unassigned,
        Instances = tagFilter.Instances,
        SyncStates = tagFilter.SyncStates
      };
    }

    public static RecordFolder Map(this Contracts.RecordFolder recordFolder)
    {
      return new RecordFolder
      {
        Year = recordFolder.Year,
        Month = recordFolder.Month,
        Day = recordFolder.Day
      };
    }

    public static Record Map(this Contracts.RecordChangeRequest record)
    {
      return new Record(record.RecordId, record.Title, string.Empty)
      {
        Album = record.Album,
        Artist = record.Artist,
        Genre = record.Genre,
        Language = record.Language,
        Locked = record.Locked,
        Groups = record.Groups
      };
    }

    public static AuditQuery Map(this Contracts.AuditFilter filter)
    {
      return new AuditQuery
      {
        Filter = filter.Filter,
        Statuses = filter.Status ?? [],
        Actions = filter.Action ?? [],
        Direction = filter.Direction,
        Instances = filter.Instance ?? [],
        From = filter.From?.UtcDateTime,
        To = filter.To?.UtcDateTime,
        Message = filter.Message,
        Sort = ParseSort(filter.Sort),
      };
    }

    private static List<AuditSort> ParseSort(IList<string>? columns)
    {
      var sort = new List<AuditSort>();
      foreach (var column in columns ?? [])
      {
        var descending = column.StartsWith('-');
        var name = descending ? column[1..] : column;
        if (Enum.TryParse<AuditSortField>(name, true, out var field)
          && Enum.IsDefined(field)
          && !int.TryParse(name, out _)
          && sort.All(s => s.Field != field))
        {
          sort.Add(new AuditSort(field, descending));
        }
      }
      return sort;
    }
  }
}
