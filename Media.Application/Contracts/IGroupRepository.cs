using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts
{
  public interface IGroupRepository
  {

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
}
