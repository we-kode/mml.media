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
  /// The track number of the record.
  /// </summary>
  public int TrackNumber { get; set; }

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
  /// Language of the record or null if no one provided.
  /// </summary>
  public string? Language { get; set; }

  /// <summary>
  /// Date when the record was last time modified.
  /// </summary>
  public DateTime Date { get; set; }

  /// <summary>
  /// The duration of the record in microseconds.
  /// </summary>
  public double Duration { get; set; }

  /// <summary>
  /// The bitrate of the record.
  /// </summary>
  public int Bitrate { get; set; }

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
  /// <param name="tracknumber">The track number of the record.</param>
  /// <param name="artist">The artists or null if no one provided.</param>
  /// <param name="date">Date when the record was last time modified.</param>
  /// <param name="duration">The duration of the record in microseconds.</param>
  /// <param name="bitrate">The bitrate of the record.</param>
  /// <param name="groups">List of groups the record is assigned to.</param>
  /// <param name="album">The album of the record.</param>
  /// <param name="genre">The genre of the record.</param>
  /// <param name="language">The language of the record.</param>
  /// <param name="checksum">The checksum of record.</param>
  public Record(Guid recordId, string title, int tracknumber, string? artist, DateTime date, TimeSpan duration, int bitrate, ICollection<Group> groups, string album = "", string genre = "", string language = "", string checksum = "")
  {
    RecordId = recordId;
    Title = title;
    TrackNumber = tracknumber;
    Artist = artist;
    Album = album;
    Genre = genre;
    Language = language;
    Date = date;
    Duration = duration.TotalMilliseconds;
    Checksum = checksum;
    Groups = groups ?? new List<Group>();
    Bitrate = bitrate;
  }

  public Record(Guid recordId, string title)
  {
    RecordId = recordId;
    Title = title;
    Groups = new List<Group>();
  }
}
