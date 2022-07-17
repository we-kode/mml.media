using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using Media.DBContext.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Media.Infrastructure
{
  public class SqlRecordsRepository : IRecordsRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;

    public SqlRecordsRepository(Func<ApplicationDBContext> contextFactory)
    {
      _contextFactory = contextFactory;
    }

    public bool IsIndexed(string checksum)
    {
      using var context = _contextFactory();
      return context.Records.Any(record => record.Checksum == checksum);
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

      record = new Records
      {
        Checksum = metaData.Checksum,
        FilePath = metaData.PhysicalFilePath,
        Date = metaData.Date,
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
          artist = new Artists
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
          genre = new Genres
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
          album = new Albums
          {
            AlbumName = metaData.Album
          };
        }
        record.Album = album;
      }

      context.Records.Add(record);
      await context.SaveChangesAsync().ConfigureAwait(false);
      scope.Complete();
    }
  }
}
