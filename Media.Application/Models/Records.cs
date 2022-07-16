using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// list of records 
/// </summary>
public class Records
{
  /// <summary>
  /// The total count of available records
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// On chunk of loaded records.
  /// </summary>
  public IList<Record> Items { get; set; }

  /// <summary>
  /// Constructs new Instance
  /// </summary>
  public Records()
  {
    Items = new List<Record>();
  }
}
