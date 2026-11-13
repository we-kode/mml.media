using Media.Application.Types;
using System;
using System.Text.Json.Serialization;

namespace Media.Application.Models.Federation;

/// <summary>
/// A partner instance records are synchronized with.
/// </summary>
public class Peer
{
  /// <summary>
  /// Id of the partner.
  /// </summary>
  public Guid PeerId { get; set; }

  /// <summary>
  /// The unique instance name of the partner.
  /// </summary>
  public string Instance { get; set; } = string.Empty;

  /// <summary>
  /// The public base url of the partner.
  /// </summary>
  public string BaseUrl { get; set; } = string.Empty;

  /// <summary>
  /// The federation endpoint of the partner.
  /// </summary>
  [JsonIgnore]
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// The url where the partner publishes its public keys.
  /// </summary>
  [JsonIgnore]
  public string JwksUri { get; set; } = string.Empty;

  /// <summary>
  /// The last known public keys of the partner as JSON web key set.
  /// </summary>
  [JsonIgnore]
  public string? Jwks { get; set; }

  /// <summary>
  /// Date when the public keys of the partner were loaded the last time.
  /// </summary>
  public DateTime? JwksUpdatedAt { get; set; }

  /// <summary>
  /// The registration state of the partner.
  /// </summary>
  public PeerState State { get; set; }

  /// <summary>
  /// The id of the registration request.
  /// </summary>
  [JsonIgnore]
  public Guid RequestId { get; set; }

  /// <summary>
  /// Date when the partner was created.
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Date when the partner was updated the last time.
  /// </summary>
  public DateTime UpdatedAt { get; set; }
}
