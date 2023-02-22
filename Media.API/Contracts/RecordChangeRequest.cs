using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

public class RecordChangeRequest
{
  /// <summary>
  /// Id of the record entry.
  /// </summary>
  [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
  public Guid RecordId { get; set; }

  /// <summary>
  /// Title of the record or null if no one provided.
  /// </summary>
  [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
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
  /// List of groups the record is assigned to.
  /// </summary>
  public ICollection<Group> Groups { get; set; }

  /// <summary>
  /// Inits a record.
  /// </summary>
  /// <param name="recordId">Id of the record entry.</param>
  /// <param name="title">Title of the record.</param>
  /// <param name="artist">The artists or null if no one provided.</param>
  /// <param name="groups">List of groups the record is assigned to.</param>
  public RecordChangeRequest(Guid recordId, string title, string? artist, ICollection<Group> groups)
  {
    RecordId = recordId;
    Title = title;
    Artist = artist;
    Groups = groups ?? new List<Group>();
  }
}
