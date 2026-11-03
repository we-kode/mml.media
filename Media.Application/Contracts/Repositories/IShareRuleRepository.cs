using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IShareRuleRepository
{
  /// <summary>
  /// Loads a chunk of the rules.
  /// </summary>
  /// <param name="filter">Text, which must be part of a group, genre, album or artist name of the rule.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of the chunk to be loaded.</param>
  ShareRules List(string? filter, int skip, int take);

  /// <summary>
  /// Loads a rule.
  /// </summary>
  /// <param name="id">Id of the rule.</param>
  /// <returns>The rule or null, if it does not exist.</returns>
  ShareRule? Load(Guid id);

  /// <summary>
  /// Creates or changes a rule. Unknown groups, genres, albums and artists are ignored.
  /// </summary>
  /// <param name="id">Id of the rule to be changed or null to create a new rule.</param>
  /// <param name="groups">Ids of the groups.</param>
  /// <param name="allRecords">True, if the rule matches all records. The criteria are not saved then.</param>
  /// <param name="genres">Ids of the genres.</param>
  /// <param name="albums">Ids of the albums.</param>
  /// <param name="artists">Ids of the artists.</param>
  /// <returns>True, if the rule was saved, false if the rule to be changed does not exist.</returns>
  Task<bool> Save(Guid? id, IList<Guid> groups, bool allRecords, IList<Guid> genres, IList<Guid> albums, IList<Guid> artists);

  /// <summary>
  /// Deletes rules.
  /// </summary>
  /// <param name="ids">Ids of the rules.</param>
  Task Delete(IList<Guid> ids);

  /// <summary>
  /// Returns the given records, which are visible for the groups and which a rule of the groups allows to share.
  /// </summary>
  /// <param name="recordIds">Ids of the records.</param>
  /// <param name="clientGroups">Groups of the client.</param>
  /// <returns>Ids of the records the client can share.</returns>
  IList<Guid> FilterShareable(IEnumerable<Guid> recordIds, IList<Guid> clientGroups);

  /// <summary>
  /// Returns true, if at least one rule exists for the groups.
  /// </summary>
  /// <param name="clientGroups">Groups of the client.</param>
  bool HasRules(IList<Guid> clientGroups);

  /// <summary>
  /// Removes rules, whose genres, albums and artists were all removed, because such rules would match every record.
  /// </summary>
  Task RemoveEmpty();

  /// <summary>
  /// Removes the rules which belong to the group only, because they become useless when the group is deleted.
  /// </summary>
  /// <param name="groupId">Id of the group.</param>
  Task RemoveTargetingOnly(Guid groupId);
}
