using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IGroupRepository
{
  /// <summary>
  /// Returns a list of all existing groups.
  /// </summary>
  /// <param name="filter">Groups will be filtered by given string.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="Groups"/></returns>
  Groups List(string? filter, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Returns the groups which are set as default.
  /// </summary>
  /// <returns><see cref="List"/> of <see cref="Guid"/> if group or null if no default group exists.</returns>
  IList<Guid>? GetDefaultGroups();

  /// <summary>
  /// Creates a new group in the db.
  /// </summary>
  /// <param name="group"><see cref="Group"/> to be created.</param>
  Task Create(Group group);

  /// <summary>
  /// Updates one group.
  /// </summary>
  /// <param name="group"><see cref="Group"/> to be updated.</param>
  Task Update(Group group);

  /// <summary>
  /// Removes one group.
  /// </summary>
  /// <param name="id">Id of group to be removed.</param>
  Task Delete(Guid id);

  /// <summary>
  /// Returns a boolean, that indicates whether a group with the given id exists.
  /// </summary>
  /// <param name="id">Id to check for.</param>
  /// <returns>Boolean, that indicates whether a group with the given id exists.</returns>
  Task<bool> GroupExists(Guid id);
}
