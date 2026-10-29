using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// Metadata of a record sent to a partner.
/// </summary>
public class SyncedRecord
{
  /// <summary>
  /// Id of the record on the owning instance.
  /// </summary>
  public Guid OwnerRecordId { get; set; }

  /// <summary>
  /// SHA-1 checksum of the original file, used as file name.
  /// </summary>
  public string Checksum { get; set; } = string.Empty;

  /// <summary>
  /// Title of the record.
  /// </summary>
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Artist of the record.
  /// </summary>
  public string? Artist { get; set; }

  /// <summary>
  /// Album of the record.
  /// </summary>
  public string? Album { get; set; }

  /// <summary>
  /// Genre of the record.
  /// </summary>
  public string? Genre { get; set; }

  /// <summary>
  /// Language of the record.
  /// </summary>
  public string? Language { get; set; }

  /// <summary>
  /// Track number of the record.
  /// </summary>
  public int TrackNumber { get; set; }

  /// <summary>
  /// Date of the record.
  /// </summary>
  public DateTime Date { get; set; }

  /// <summary>
  /// Duration of the record.
  /// </summary>
  public TimeSpan Duration { get; set; }

  /// <summary>
  /// Bitrate of the record in kbit/s.
  /// </summary>
  public int Bitrate { get; set; }
}
