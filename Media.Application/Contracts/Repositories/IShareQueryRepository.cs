using System;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IShareQueryRepository
{
  /// <summary>
  /// Returns the token of the filters. Equal filters share the same token.
  /// </summary>
  /// <param name="queryHash">Hash of the canonical filters.</param>
  /// <param name="query">The canonical filters as json.</param>
  /// <param name="newToken">The token to be used, if the filters are not stored yet.</param>
  /// <param name="now">Current date.</param>
  Task<string> GetOrCreate(string queryHash, string query, string newToken, DateTime now);

  /// <summary>
  /// Returns the filters of the token and marks the token as used.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <param name="now">Current date.</param>
  /// <returns>The canonical filters as json or null, if the token is unknown.</returns>
  string? Find(string token, DateTime now);

  /// <summary>
  /// Removes the tokens, which were not used since the given date.
  /// </summary>
  /// <param name="before">Tokens not used since this date are removed.</param>
  Task RemoveUnused(DateTime before);
}
