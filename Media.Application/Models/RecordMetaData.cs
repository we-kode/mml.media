using System;

namespace Media.Application.Models
{
  public class RecordMetaData
  {
    /// <summary>
    /// Title of the record or null if no one provided.
    /// </summary>
    public string? Title { get; set; }

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
    /// Front cover of record.
    /// </summary>
    public string? Cover { get; set; }

    /// <summary>
    /// The tracknumber of the record.
    /// </summary>
    public int TrackNumber { get; set; }

    /// <summary>
    /// MimeType of the file. Default is audio/mpeg.
    /// </summary>
    public string MimeType { get; set; } = "audio/mpeg";

    /// <summary>
    /// Date when the record was last time modified.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The duration of the record.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// The filename without extension of the file which was uploaded.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Path where the file is stored on server.
    /// </summary>
    public string PhysicalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Sha1 hash value of the file content.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// The compressed bitrate of the record.
    /// </summary>
    public int Bitrate {  get; set; }
  }
}
