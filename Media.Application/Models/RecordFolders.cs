using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// List of record folders.
/// </summary>
public class RecordFolders
{
  /// <summary>
  /// The total count of available fodlers.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded folders.
  /// </summary>
  public IList<RecordFolder> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public RecordFolders()
  {
    Items = new List<RecordFolder>();
  }
}
