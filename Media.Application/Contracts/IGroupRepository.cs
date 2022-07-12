using Media.Application.Models;
using System;
using System.Threading.Tasks;

namespace Media.Application.Contracts
{
  public interface IGroupRepository
  {

    /// <summary>
    /// Returns the group which is set as default.
    /// </summary>
    /// <returns><see cref="Guid"/> ipf group or null if no default group exists.</returns>
    Guid? GetDefaultGroup();

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
  }
}
