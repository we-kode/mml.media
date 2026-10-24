using System;
using System.Text.RegularExpressions;

namespace Media.Application.Models.Federation;

/// <summary>
/// Configuration of the federation with partner instances.
/// </summary>
public partial class FederationOptions
{
  /// <summary>
  /// Path of the federation endpoint relative to the public url.
  /// </summary>
  public const string EndpointPath = "/api/v2.0/media/federation/peer";

  /// <summary>
  /// Version of the federation protocol.
  /// </summary>
  public const string ProtocolVersion = "1";

  /// <summary>
  /// The public base url partners reach this instance at. Defaults to the issuer of the identity service, see <see cref="UseIssuerAsDefault"/>.
  /// </summary>
  public string? PublicUrl { get; set; }

  /// <summary>
  /// Allows partners in private or loopback networks. Only intended for tests.
  /// </summary>
  public bool AllowPrivateNetworks { get; set; }

  /// <summary>
  /// Folder the federation keys are stored in.
  /// </summary>
  public string KeysPath { get; set; } = "/secrets/federation";

  /// <summary>
  /// Days a signing key stays active before it is replaced.
  /// </summary>
  public int KeyRotationDays { get; set; } = 90;

  /// <summary>
  /// Days a new key is published before it is used for signing.
  /// </summary>
  public int KeyPublishLeadDays { get; set; } = 7;

  /// <summary>
  /// Days a replaced key stays published.
  /// </summary>
  public int KeyRetireDays { get; set; } = 14;

  /// <summary>
  /// Size in megabytes of one chunk a record is sent to a partner with. Must be lower than the upload limit of the partner's reverse proxy.
  /// </summary>
  public int ChunkSizeMb { get; set; } = 32;

  /// <summary>
  /// Maximum size in megabytes of a record received from a partner.
  /// </summary>
  public int MaxRecordSizeMb { get; set; } = 2048;

  /// <summary>
  /// Folder incomplete transfers from partners are stored in until they are finished.
  /// </summary>
  public string UploadPath { get; set; } = "/tmp/records/federation";

  /// <summary>
  /// Days an incomplete transfer from a partner is kept without progress.
  /// </summary>
  public int UploadRetentionDays { get; set; } = 7;

  /// <summary>
  /// Maximum count of incomplete transfers of one partner. Further transfers are rejected until one is finished
  /// or removed after <see cref="UploadRetentionDays"/>.
  /// </summary>
  public int MaxOpenTransfers { get; set; } = 10;

  /// <summary>
  /// Sets the <see cref="PublicUrl"/> to the given issuer, if no public url is configured.
  /// The issuer is the public address of the reverse proxy, which partners use as well.
  /// </summary>
  /// <param name="issuer">The issuer of the identity service.</param>
  /// <returns>The options.</returns>
  public FederationOptions UseIssuerAsDefault(string? issuer)
  {
    if (string.IsNullOrWhiteSpace(PublicUrl))
    {
      PublicUrl = issuer?.TrimEnd('/');
    }

    return this;
  }

  /// <summary>
  /// Indicates whether the federation is enabled, which is the case as soon as a public url is known.
  /// </summary>
  public bool IsEnabled => !string.IsNullOrWhiteSpace(PublicUrl);

  /// <summary>
  /// The federation endpoint of this instance.
  /// </summary>
  public string Endpoint => $"{PublicUrl?.TrimEnd('/')}{EndpointPath}";

  /// <summary>
  /// The url the public keys of this instance are published at.
  /// </summary>
  public string JwksUri => $"{Endpoint}/jwks";

  /// <summary>
  /// Returns the audience used in assertions sent to the given instance.
  /// </summary>
  /// <param name="instance">The name of the receiving instance.</param>
  /// <returns>The audience value.</returns>
  public static string Audience(string instance) => $"wekode.mml.federation/{instance.ToLowerInvariant()}";

  /// <summary>
  /// Returns whether the name can be used as instance name of a partner.
  /// </summary>
  /// <param name="instance">The instance name.</param>
  /// <returns>True, if the name contains 1 to 64 letters, digits, dots, hyphens or underscores.</returns>
  public static bool IsValidInstanceName(string? instance) => !string.IsNullOrEmpty(instance) && InstanceNamePattern().IsMatch(instance);

  /// <summary>
  /// Ensures that the name of this instance can be used by partners, if the federation is enabled.
  /// </summary>
  /// <param name="instance">The name of this instance.</param>
  /// <exception cref="InvalidOperationException">If the federation is enabled and the instance name is invalid.</exception>
  public void EnsureValidInstance(string? instance)
  {
    if (IsEnabled && !IsValidInstanceName(instance))
    {
      throw new InvalidOperationException($"The instance name '{instance}' cannot be used for federation. It must contain 1 to 64 letters, digits, dots, hyphens or underscores.");
    }
  }

  [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
  private static partial Regex InstanceNamePattern();

  /// <summary>
  /// Returns the discovery url of a partner with the given base url.
  /// </summary>
  /// <param name="baseUrl">The base url of the partner.</param>
  /// <returns>The discovery url.</returns>
  public static Uri DiscoveryUri(string baseUrl) => new($"{baseUrl.TrimEnd('/')}{EndpointPath}/discovery");
}
