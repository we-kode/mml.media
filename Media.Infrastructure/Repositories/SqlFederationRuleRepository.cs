using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBRule = Media.DBContext.Models.FederationRule;

namespace Media.Infrastructure.Repositories;

public class SqlFederationRuleRepository(Func<ApplicationDBContext> contextFactory) : IFederationRuleRepository
{
  public Rules List(SyncDirection? direction = null)
  {
    using var context = contextFactory();
    return ToRules(WithReferences(context.FederationRules)
      .Where(r => direction == null || r.Direction == (int)direction));
  }

  public Rules ListFor(Guid peerId, SyncDirection? direction = null)
  {
    using var context = contextFactory();
    return ToRules(WithReferences(context.FederationRules)
      .Where(r => direction == null || r.Direction == (int)direction)
      .Where(r => !r.Peers.Any() || r.Peers.Any(p => p.PeerId == peerId)));
  }

  public Rule? Get(Guid ruleId)
  {
    using var context = contextFactory();
    var rule = WithReferences(context.FederationRules).FirstOrDefault(r => r.RuleId == ruleId);
    return rule == null ? null : Map(rule);
  }

  public async Task<Rule> Create(Rule rule)
  {
    using var context = contextFactory();
    var dbRule = new DBRule
    {
      CreatedAt = DateTime.UtcNow,
    };
    Apply(context, dbRule, rule);
    context.FederationRules.Add(dbRule);
    await context.SaveChangesAsync().ConfigureAwait(false);
    return Map(dbRule);
  }

  public async Task Update(Rule rule)
  {
    using var context = contextFactory();
    var dbRule = WithReferences(context.FederationRules).FirstOrDefault(r => r.RuleId == rule.RuleId);
    if (dbRule == null)
    {
      return;
    }

    var previous = References([dbRule]);
    Apply(context, dbRule, rule);
    await context.SaveChangesAsync().ConfigureAwait(false);
    await RemoveUnused(context, previous).ConfigureAwait(false);
  }

  public async Task Delete(IEnumerable<Guid> ruleIds)
  {
    using var context = contextFactory();
    var ids = ruleIds.ToList();
    var rules = WithReferences(context.FederationRules).Where(r => ids.Contains(r.RuleId)).ToList();
    var previous = References(rules);
    context.FederationRules.RemoveRange(rules);
    await context.SaveChangesAsync().ConfigureAwait(false);
    await RemoveUnused(context, previous).ConfigureAwait(false);
  }

  private static (List<Guid> Artists, List<Guid> Albums, List<Guid> Genres) References(IEnumerable<DBRule> rules)
  {
    var list = rules.ToList();
    return (
      [.. list.SelectMany(r => r.Artists).Select(a => a.ArtistId).Distinct()],
      [.. list.SelectMany(r => r.Albums).Select(a => a.AlbumId).Distinct()],
      [.. list.SelectMany(r => r.Genres).Select(g => g.GenreId).Distinct()]);
  }

  // artists, albums and genres are kept while a rule references them, so they are removed here once unused.
  private static async Task RemoveUnused(ApplicationDBContext context, (List<Guid> Artists, List<Guid> Albums, List<Guid> Genres) ids)
  {
    context.Artists.RemoveRange(context.Artists.Where(a => ids.Artists.Contains(a.ArtistId)
      && !a.Records.Any() && !context.FederationRules.Any(r => r.Artists.Any(x => x.ArtistId == a.ArtistId))));
    context.Albums.RemoveRange(context.Albums.Where(a => ids.Albums.Contains(a.AlbumId)
      && !a.Records.Any() && !context.FederationRules.Any(r => r.Albums.Any(x => x.AlbumId == a.AlbumId))));
    context.Genres.RemoveRange(context.Genres.Where(g => ids.Genres.Contains(g.GenreId) && g.Bitrate == null
      && !g.Records.Any() && !context.FederationRules.Any(r => r.Genres.Any(x => x.GenreId == g.GenreId))));
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  private static void Apply(ApplicationDBContext context, DBRule dbRule, Rule rule)
  {
    dbRule.Direction = (int)rule.Direction;
    var peerIds = rule.Peers.Select(p => p.PeerId).Distinct().ToList();
    Replace(dbRule.Peers, [.. context.FederationPeers.Where(p => peerIds.Contains(p.PeerId))], peerIds.Count);
    var artistIds = rule.Artists.Select(a => a.ArtistId).Distinct().ToList();
    var albumIds = rule.Albums.Select(a => a.AlbumId).Distinct().ToList();
    var genreIds = rule.Genres.Select(g => g.GenreId).Distinct().ToList();
    Replace(dbRule.Artists, [.. context.Artists.Where(a => artistIds.Contains(a.ArtistId))], artistIds.Count);
    Replace(dbRule.Albums, [.. context.Albums.Where(a => albumIds.Contains(a.AlbumId))], albumIds.Count);
    Replace(dbRule.Genres, [.. context.Genres.Where(g => genreIds.Contains(g.GenreId))], genreIds.Count);

    var groupIds = rule.Groups.Select(g => g.Id).Distinct().ToList();
    dbRule.Groups.Clear();
    foreach (var group in context.Groups.Where(g => groupIds.Contains(g.GroupId)))
    {
      dbRule.Groups.Add(group);
    }
  }

  private static Rules ToRules(IQueryable<DBRule> rules)
  {
    var items = rules.OrderBy(r => r.CreatedAt).AsEnumerable().Select(Map).ToList();
    return new Rules { TotalCount = items.Count, Items = items };
  }

  private static IQueryable<DBRule> WithReferences(IQueryable<DBRule> rules)
  {
    return rules
      .Include(r => r.Peers)
      .Include(r => r.Groups)
      .Include(r => r.Artists)
      .Include(r => r.Albums)
      .Include(r => r.Genres)
      .AsSplitQuery();
  }

  private static void Replace<T>(ICollection<T> target, IList<T> items, int expectedCount)
  {
    if (items.Count != expectedCount)
    {
      throw new FederationException(FederationErrors.InvalidRule);
    }

    target.Clear();
    foreach (var item in items)
    {
      target.Add(item);
    }
  }

  private static Rule Map(DBRule rule)
  {
    return new Rule
    {
      RuleId = rule.RuleId,
      Peers = [.. rule.Peers.Select(p => new Peer { PeerId = p.PeerId, Instance = p.Instance, BaseUrl = p.BaseUrl, State = (PeerState)p.State })],
      Direction = (SyncDirection)rule.Direction,
      Artists = [.. rule.Artists.Select(a => new Artist { ArtistId = a.ArtistId, Name = a.Name })],
      Albums = [.. rule.Albums.Select(a => new Album { AlbumId = a.AlbumId, AlbumName = a.AlbumName })],
      Genres = [.. rule.Genres.Select(g => new Genre { GenreId = g.GenreId, Name = g.Name })],
      Groups = [.. rule.Groups.Select(g => new Group(g.GroupId, g.Name, g.IsDefault))],
      CreatedAt = rule.CreatedAt,
    };
  }
}
