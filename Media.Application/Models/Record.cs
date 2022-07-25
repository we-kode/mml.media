using System;
using System.Collections.Generic;

namespace Media.Application.Models;

/// <summary>
/// Represents a record in the application
/// </summary>
public class Record
{
  /// <summary>
  /// Id of the record entry.
  /// </summary>
  public Guid RecordId { get; }

  /// <summary>
  /// Title of the record or null if no one provided.
  /// </summary>
  public string Title { get; }

  /// <summary>
  /// The artists or null if no one provided.
  /// </summary>
  public string? Artist { get; }

  /// <summary>
  /// Genre of the record or null if no one provided.
  /// </summary>
  public string? Genre { get; }

  /// <summary>
  /// Album of the record or null if no one provided.
  /// </summary>
  public string? Album { get; }

  /// <summary>
  /// Date when the record was last time modified.
  /// </summary>
  public DateTime Date { get; }

  /// <summary>
  /// The duration of the record in microseconds.
  /// </summary>
  public double Duration { get; set; }

  /// <summary>
  /// List of groups the record is assigned to.
  /// </summary>
  public ICollection<Group> Groups { get; set; }

  /// <summary>
  /// Inits a record.
  /// </summary>
  /// <param name="recordId">Id of the record entry.</param>
  /// <param name="title">Title of the record.</param>
  /// <param name="artist">The artists or null if no one provided.</param>
  /// <param name="date">Date when the record was last time modified.</param>
  /// <param name="duration">The duration of the record in microseconds.</param>
  /// <param name="groups">List of groups the record is assigned to.</param>
  public Record(Guid recordId, string title, string? artist, DateTime date, TimeSpan duration, ICollection<Group> groups)
  {
    RecordId = recordId;
    Title = title;
    Artist = artist;
    Date = date;
    Duration = duration.TotalMilliseconds;
    Groups = groups ?? new List<Group>();
  }
}
