﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Media.DBContext.Models;

/// <summary>
/// Represents the available groups, one record can be assigned.
/// </summary>
[Table("groups")]
public class Group
{
  /// <summary>
  /// Primary Key. Autogenerated.
  /// </summary>
  [Key]
  public Guid GroupId { get; set; }

  /// <summary>
  /// Name of the group.
  /// </summary>
  public string Name { get; set; } = null!;

  /// <summary>
  /// True, if group will be assigned default to new records.
  /// </summary>
  public bool IsDefault { get; set; }

  /// <summary>
  /// List of records, which belong to the group.
  /// </summary>
  public ICollection<Record> Records { get; set; } = new List<Record>();

  /// <summary>
  /// List of livestreams, which belong to the group.
  /// </summary>
  public ICollection<Livestream> Livestreams { get; set; } = new List<Livestream>();
}
