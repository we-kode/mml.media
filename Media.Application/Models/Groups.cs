using System.Collections.Generic;

namespace Media.Application.Models
{
  /// <summary>
  /// A list of groups.
  /// </summary>
  public class Groups
  {
    /// <summary>
    /// The total count of available groups.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// On chunk of loaded group.
    /// </summary>
    public IList<Group> Items { get; set; }

    /// <summary>
    /// Constructs new instance.
    /// </summary>
    public Groups()
    {
      Items = new List<Group>();
    }
  }
}
