﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.DBContext.Models;

/// <summary>
/// Represents the artists available in records.
/// </summary>
public class Artists
{
  /// <summary>
  /// Primary key of the entity. Autogenerated.
  /// </summary>
  [Key]
  public Guid ArtistId { get; set; }

  /// <summary>
  /// Name of the artist.
  /// </summary>
  public string Name { get; set; } = null!;

  /// <summary>
  /// List of records, which belong to the artist.
  /// </summary>
  public ICollection<Records> Records { get; set; } = new List<Records>();
}
