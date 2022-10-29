namespace Media.API.Contracts;

/// <summary>
/// Represents a folder group of one record.
/// </summary>
public class RecordFolder
{
  /// <summary>
  /// The year the folder group belongs to.
  /// </summary>
  public int Year { get; set; }

  /// <summary>
  /// The month the folder group belongs to. Null if group is on the year stage.
  /// </summary>
  public int? Month { get; set; }

  /// <summary>
  /// The day the folder group belongs to. Null if group is on the month stage.
  /// </summary>
  public int? Day { get; set; }
}
