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
  public Guid RecordId { get; set; }

  /// <summary>
  /// Title of the record or null if no one provided.
  /// </summary>
  public string Title { get; set; }

  /// <summary>
  /// The artists or null if no one provided.
  /// </summary>
  public string? Artist { get; set; }

  /// <summary>
  /// Genre of the record or null if no one provided.
  /// </summary>
  public string? Genre { get; set; }

  /// <summary>
  /// Album of the record or null if no one provided.
  /// </summary>
  public string? Album { get; set; }

  /// <summary>
  /// Date when the record was last time modified.
  /// </summary>
  public DateTime Date { get; set; }

  /// <summary>
  /// The duration of the record in microseconds.
  /// </summary>
  public double Duration { get; set; }

  public string? Checksum { get; set; }

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
  /// <param name="album">The album of the record.</param>
  /// <param name="genre">The genre of the record.</param>
  /// <param name="checksum">The checksum of record.</param>
  public Record(Guid recordId, string title, string? artist, DateTime date, TimeSpan duration, ICollection<Group> groups, string album = "", string genre = "", string checksum = "")
  {
    RecordId = recordId;
    Title = title;
    Artist = artist;
    Album = album;
    Genre = genre;
    Date = date;
    Duration = duration.TotalMilliseconds;
    Checksum = checksum;
    Groups = groups ?? new List<Group>();
  }

  public Record(Guid recordId, string title)
  {
    RecordId = recordId;
    Title = title;
    Groups = new List<Group>();
  }
}
