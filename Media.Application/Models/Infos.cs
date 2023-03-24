using System.Collections.Generic;

namespace Media.Application.Models;


/// <summary>
/// List of infos
/// </summary>
public class Infos
{
  /// <summary>
  /// The total count of available items.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded items.
  /// </summary>
  public IList<Info> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public Infos() => Items = new List<Info>();
}
