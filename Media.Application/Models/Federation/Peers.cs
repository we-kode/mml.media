using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A list of partner instances.
/// </summary>
public class Peers
{
  /// <summary>
  /// The total count of available partners.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded partners.
  /// </summary>
  public IList<Peer> Items { get; set; } = [];
}
