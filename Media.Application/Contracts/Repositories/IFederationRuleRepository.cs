using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationRuleRepository
{
  /// <summary>
  /// Returns all rules, oldest first.
  /// </summary>
  /// <param name="direction">Only rules of this direction are returned if set.</param>
  /// <returns><see cref="Rules"/></returns>
  Rules List(SyncDirection? direction = null);

  /// <summary>
  /// Returns the rules which apply to a partner, i.e. the rules for all partners and the rules restricted to the partner, oldest first.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="direction">Only rules of this direction are returned if set.</param>
  /// <returns><see cref="Rules"/></returns>
  Rules ListFor(Guid peerId, SyncDirection? direction = null);

  /// <summary>
  /// Returns one rule or null if it does not exist.
  /// </summary>
  /// <param name="ruleId">Id of the rule.</param>
  /// <returns><see cref="Rule"/> or null.</returns>
  Rule? Get(Guid ruleId);

  /// <summary>
  /// Creates a new rule. Unknown groups are ignored.
  /// Unknown partners, artists, albums or genres are rejected.
  /// </summary>
  /// <param name="rule">The rule to be created.</param>
  /// <returns>The created <see cref="Rule"/>.</returns>
  Task<Rule> Create(Rule rule);

  /// <summary>
  /// Updates direction, partners, criteria and groups of a rule.
  /// </summary>
  /// <param name="rule">The rule to be updated.</param>
  Task Update(Rule rule);

  /// <summary>
  /// Removes the given rules.
  /// </summary>
  /// <param name="ruleIds">Ids of the rules.</param>
  Task Delete(IEnumerable<Guid> ruleIds);
}
