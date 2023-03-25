﻿using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Media.DBContext.Models;

public class Livestreams
{
  /// <summary>
  /// Primary Key. Autogenerated.
  /// </summary>
  [Key]
  public Guid LivestreamId { get; set; }

  /// <summary>
  /// Name to be displayed.
  /// </summary>
  [Required]
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// The internal url of the streaming provider endpoint.
  /// </summary>
  public string? Url { get; set; }

  /// <summary>
  /// The type of the streaming provider endpoint.
  /// </summary>
  public int ProviderType { get; set; }

  /// <summary>
  /// The groups asssociated with this stream.
  /// </summary>
  public ICollection<Groups> Groups { get; set; } = new List<Groups>();

}
