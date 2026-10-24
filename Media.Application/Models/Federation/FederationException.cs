using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// Error raised by a federation operation. The code is returned to the admin app.
/// </summary>
/// <param name="code">One of the <see cref="FederationErrors"/> codes.</param>
/// <param name="message">Optional technical details.</param>
public class FederationException(string code, string? message = null) : Exception(message ?? code)
{
  /// <summary>
  /// The error code.
  /// </summary>
  public string Code { get; } = code;
}

/// <summary>
/// Error codes of federation operations.
/// </summary>
public static class FederationErrors
{
  /// <summary>
  /// The federation is not configured.
  /// </summary>
  public const string Disabled = "FEDERATION_DISABLED";
  /// <summary>
  /// The url is not an absolute https url.
  /// </summary>
  public const string InvalidUrl = "INVALID_URL";
  /// <summary>
  /// The partner could not be reached.
  /// </summary>
  public const string RemoteUnreachable = "REMOTE_UNREACHABLE";
  /// <summary>
  /// The partner rejected the request.
  /// </summary>
  public const string RemoteRejected = "REMOTE_REJECTED";
  /// <summary>
  /// The partner returned invalid public information.
  /// </summary>
  public const string InvalidDiscovery = "INVALID_DISCOVERY";
  /// <summary>
  /// The instance name is already used.
  /// </summary>
  public const string InstanceConflict = "INSTANCE_CONFLICT";
  /// <summary>
  /// The partner is not in the required state.
  /// </summary>
  public const string InvalidState = "INVALID_STATE";
  /// <summary>
  /// The signed assertion is invalid.
  /// </summary>
  public const string InvalidAssertion = "INVALID_ASSERTION";
  /// <summary>
  /// The invitation code is unknown, used or expired.
  /// </summary>
  public const string InvalidInvitation = "INVALID_INVITATION";
  /// <summary>
  /// The sent record, its metadata or its cover are invalid.
  /// </summary>
  public const string InvalidRecord = "INVALID_RECORD";
  /// <summary>
  /// A value of the rule is too long.
  /// </summary>
  public const string InvalidRule = "INVALID_RULE";
  /// <summary>
  /// The partner has too many open transfers. The transfer is retried later.
  /// </summary>
  public const string TooManyTransfers = "TOO_MANY_TRANSFERS";
  /// <summary>
  /// The partner does not exist.
  /// </summary>
  public const string NotFound = "NOT_FOUND";
}
