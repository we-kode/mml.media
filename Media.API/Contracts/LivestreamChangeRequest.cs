﻿using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts
{
  public class LivestreamChangeRequest
  { /// <summary>
    /// Primary Key. Autogenerated.
    /// </summary>
    public Guid? LivestreamId { get; set; }

    /// <summary>
    /// Name to be displayed.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The internal url of the streaming provider endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// List of groups the record is assigned to.
    /// </summary>
    public ICollection<Group> Groups { get; set; }

    public LivestreamChangeRequest(Guid? livestreamId, string displayName, string? url, ICollection<Group> groups)
    {
      LivestreamId = livestreamId;
      DisplayName = displayName;
      Url = url;
      Groups = groups ?? new List<Group>();
    }
  }
}
