using System;
using System.Collections.Generic;

namespace Media.API.Contracts
{
  /// <summary>
  /// Request for assignment to groups
  /// </summary>
  public class AssignmentRequest
  {
    /// <summary>
    /// Items to be assigned.
    /// </summary>
    public List<Guid> Items { get; set; } = new List<Guid>();

    /// <summary>
    /// Listz of groups to which the items should be assigned.
    /// </summary>
    public List<Guid> Groups { get; set; } = new List<Guid>();
  }
}
