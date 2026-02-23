using Media.Application.Contracts.Repositories;
using Media.Application.Models;

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
        EndDate = tagFilter.EndDate
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
      return new Record(record.RecordId, record.Title)
      {
        Album = record.Album,
        Artist = record.Artist,
        Genre = record.Genre,
        Language = record.Language,
        Locked = record.Locked,
        Cover = record.Cover,
        Groups = record.Groups
      };
    }
  }
}
