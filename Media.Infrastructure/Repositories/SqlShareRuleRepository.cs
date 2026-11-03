using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBShareRule = Media.DBContext.Models.ShareRule;

namespace Media.Infrastructure.Repositories;

public class SqlShareRuleRepository(Func<ApplicationDBContext> contextFactory) : IShareRuleRepository
{
  public ShareRules List(string? filter, int skip, int take)
  {
    using var context = contextFactory();
    var query = context.ShareRules
      .Where(r => string.IsNullOrEmpty(filter)
        || r.Groups.Any(g => EF.Functions.ILike(g.Name, $"%{filter}%"))
        || r.Genres.Any(g => EF.Functions.ILike(g.Name, $"%{filter}%"))
        || r.Albums.Any(a => EF.Functions.ILike(a.AlbumName, $"%{filter}%"))
        || r.Artists.Any(a => EF.Functions.ILike(a.Name, $"%{filter}%")));

    return new ShareRules
    {
      TotalCount = query.Count(),
      Items = [.. Include(query)
        .OrderBy(r => r.Groups.Min(g => g.Name))
        .ThenBy(r => r.ShareRuleId)
        .Skip(skip)
        .Take(take)
        .AsEnumerable()
        .Select(Map)],
    };
  }

  public ShareRule? Load(Guid id)
  {
    using var context = contextFactory();
    var rule = Include(context.ShareRules).FirstOrDefault(r => r.ShareRuleId == id);
    return rule == null ? null : Map(rule);
  }

  public async Task<bool> Save(Guid? id, IList<Guid> groups, bool allRecords, IList<Guid> genres, IList<Guid> albums, IList<Guid> artists)
  {
    using var context = contextFactory();
    DBShareRule? rule;
    if (id == null)
    {
      rule = new DBShareRule();
      context.ShareRules.Add(rule);
    }
    else
    {
      rule = await Include(context.ShareRules).FirstOrDefaultAsync(r => r.ShareRuleId == id).ConfigureAwait(false);
      if (rule == null)
      {
        return false;
      }
    }

    if (allRecords)
    {
      genres = albums = artists = [];
    }

    rule.AllRecords = allRecords;
    rule.Groups = [.. context.Groups.Where(g => groups.Contains(g.GroupId))];
    rule.Genres = [.. context.Genres.Where(g => genres.Contains(g.GenreId))];
    rule.Albums = [.. context.Albums.Where(a => albums.Contains(a.AlbumId))];
    rule.Artists = [.. context.Artists.Where(a => artists.Contains(a.ArtistId))];
    await context.SaveChangesAsync().ConfigureAwait(false);
    return true;
  }

  public async Task Delete(IList<Guid> ids)
  {
    using var context = contextFactory();
    await context.ShareRules
      .Where(r => ids.Contains(r.ShareRuleId))
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  public IList<Guid> FilterShareable(IEnumerable<Guid> recordIds, IList<Guid> clientGroups)
  {
    using var context = contextFactory();
    var ids = recordIds.Distinct().ToList();
    return [.. context.Records
      .Where(rec => ids.Contains(rec.RecordId)
        && rec.Groups.Any(g => clientGroups.Contains(g.GroupId))
        && context.ShareRules.Any(rule => rule.Groups.Any(g => clientGroups.Contains(g.GroupId))
          && (rule.AllRecords
            || ((!rule.Genres.Any() || rule.Genres.Any(g => g.GenreId == rec.GenreId))
              && (!rule.Albums.Any() || rule.Albums.Any(a => a.AlbumId == rec.AlbumId))
              && (!rule.Artists.Any() || rule.Artists.Any(a => a.ArtistId == rec.ArtistId))))))
      .Select(rec => rec.RecordId)];
  }

  public bool HasRules(IList<Guid> clientGroups)
  {
    using var context = contextFactory();
    return context.ShareRules.Any(r => r.Groups.Any(g => clientGroups.Contains(g.GroupId)));
  }

  public async Task RemoveEmpty()
  {
    using var context = contextFactory();
    await context.ShareRules
      .Where(r => !r.AllRecords && !r.Genres.Any() && !r.Albums.Any() && !r.Artists.Any())
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  public async Task RemoveTargetingOnly(Guid groupId)
  {
    using var context = contextFactory();
    await context.ShareRules
      .Where(r => r.Groups.Any(g => g.GroupId == groupId) && r.Groups.All(g => g.GroupId == groupId))
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }

  private static IQueryable<DBShareRule> Include(IQueryable<DBShareRule> query)
  {
    return query
      .Include(r => r.Groups)
      .Include(r => r.Genres)
      .Include(r => r.Albums)
      .Include(r => r.Artists)
      .AsSplitQuery();
  }

  private static ShareRule Map(DBShareRule rule)
  {
    return new ShareRule
    {
      Id = rule.ShareRuleId,
      AllRecords = rule.AllRecords,
      Groups = [.. rule.Groups.OrderBy(g => g.Name).Select(g => new Group(g.GroupId, g.Name, g.IsDefault))],
      Genres = [.. rule.Genres.OrderBy(g => g.Name).Select(g => new Genre { GenreId = g.GenreId, Name = g.Name })],
      Albums = [.. rule.Albums.OrderBy(a => a.AlbumName).Select(a => new Album { AlbumId = a.AlbumId, AlbumName = a.AlbumName })],
      Artists = [.. rule.Artists.OrderBy(a => a.Name).Select(a => new Artist { ArtistId = a.ArtistId, Name = a.Name })],
    };
  }
}
