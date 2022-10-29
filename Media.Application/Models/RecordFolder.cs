using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Application.Models;

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

  /// <summary>
  /// Inits one record folder.
  /// </summary>
  /// <param name="year">The year the folder group belongs to.</param>
  /// <param name="month">The month the folder group belongs to. Null if group is on the year stage.</param>
  /// <param name="day">The day the folder group belongs to. Null if group is on the month stage.</param>
  public RecordFolder(int year, int? month, int? day)
  {
    Year = year;
    Month = month;
    Day = day;
  }

  /// <summary>
  /// Inits one record folder.
  /// </summary>
  public RecordFolder()
  {
  }

  /// <summary>
  /// Returns a tuple of utc start and end dates this folder includes.
  /// </summary>
  /// <returns><see cref="Tuple"/> of two <see cref="DateTime"/> values.</returns>
  public Tuple<DateTime, DateTime> ToDateRange()
  {
    var start = new DateTime(Year, Month ?? 1, Day ?? 1);
    var end = new DateTime(Year, Month ?? 12, Day ?? DateTime.DaysInMonth(Year, Month ?? 12));
    return new Tuple<DateTime, DateTime>(start.ToUniversalTime(), end.ToUniversalTime());
  }
}
