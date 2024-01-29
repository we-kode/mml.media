using System;
using System.Collections.Generic;

namespace Media.API.Contracts
{
  /// <summary>
  /// Request for assignment to groups
  /// </summary>
  public class AssignFolderRequest
  {
    /// <summary>
    /// Items to be assigned.
    /// </summary>
    public List<RecordFolder> Items { get; set; } = new List<RecordFolder>();

    /// <summary>
    /// List of groups to which the items should be assigned.
    /// </summary>
    public List<Guid> Groups { get; set; } = new List<Guid>();
  }
}
