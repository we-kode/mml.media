using Media.Application.Contracts.Repositories;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DBShareQuery = Media.DBContext.Models.ShareQuery;

namespace Media.Infrastructure.Repositories;

public class SqlShareQueryRepository(Func<ApplicationDBContext> contextFactory) : IShareQueryRepository
{
  public async Task<string> GetOrCreate(string queryHash, string query, string newToken, DateTime now)
  {
    using var context = contextFactory();
    var existing = await context.ShareQueries.FirstOrDefaultAsync(q => q.QueryHash == queryHash).ConfigureAwait(false);
    if (existing != null)
    {
      existing.LastUsedAt = now;
      await context.SaveChangesAsync().ConfigureAwait(false);
      return existing.Token;
    }

    context.ShareQueries.Add(new DBShareQuery
    {
      Token = newToken,
      QueryHash = queryHash,
      Query = query,
      CreatedAt = now,
      LastUsedAt = now,
    });

    try
    {
      await context.SaveChangesAsync().ConfigureAwait(false);
      return newToken;
    }
    catch (DbUpdateException)
    {
      // The same filters were stored in parallel.
      using var retry = contextFactory();
      return (await retry.ShareQueries.FirstAsync(q => q.QueryHash == queryHash).ConfigureAwait(false)).Token;
    }
  }

  public string? Find(string token, DateTime now)
  {
    using var context = contextFactory();
    var queries = context.ShareQueries.Where(q => q.Token == token);
    var query = queries.Select(q => q.Query).FirstOrDefault();
    if (query != null)
    {
      queries.ExecuteUpdate(s => s.SetProperty(q => q.LastUsedAt, now));
    }

    return query;
  }

  public async Task RemoveUnused(DateTime before)
  {
    using var context = contextFactory();
    await context.ShareQueries
      .Where(q => q.LastUsedAt < before)
      .ExecuteDeleteAsync()
      .ConfigureAwait(false);
  }
}
