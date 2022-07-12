using System;

namespace Media.Application.Models;

public class Group
{
  /// <summary>
  /// Id of the group.
  /// </summary>
  public Guid Id { get; }

  /// <summary>
  /// Name of the group.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Indicates whether the group gets assigned automatically
  /// to a new created client or not.
  /// </summary>
  public bool IsDefault { get; }

  /// <summary>
  /// Inits a group.
  /// </summary>
  /// <param name="id">Id of the group.</param>
  /// <param name="name">Name of the group.</param>
  /// <param name="isDefault">Flag whether the group is default or not.</param>
  public Group(Guid id, string name, bool isDefault)
  {
    Id = id;
    Name = name;
    IsDefault = isDefault;
  }
}
