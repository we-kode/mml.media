using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Creates and validates the signed assertions instances authenticate each federation request with.
/// </summary>
public interface IFederationAssertionService
{
  /// <summary>
  /// Creates a short living assertion signed with the active key of this instance.
  /// </summary>
  /// <param name="targetInstance">Instance name of the receiving partner.</param>
  /// <param name="claims">Additional claims.</param>
  /// <returns>The signed assertion.</returns>
  string Create(string targetInstance, IDictionary<string, object>? claims = null);

  /// <summary>
  /// Returns an unvalidated claim of an assertion or null if it cannot be read. Only use it to find the keys the assertion is validated with.
  /// </summary>
  /// <param name="assertion">The assertion.</param>
  /// <param name="claim">The name of the claim.</param>
  /// <returns>The claim value or null.</returns>
  string? ReadClaim(string assertion, string claim);

  /// <summary>
  /// Validates signature, issuer, audience, lifetime and replay of an assertion.
  /// </summary>
  /// <param name="assertion">The assertion.</param>
  /// <param name="issuer">The expected issuer instance.</param>
  /// <param name="jwks">The public keys of the issuer.</param>
  /// <returns>The validation result.</returns>
  Task<AssertionResult> Validate(string assertion, string issuer, string? jwks);
}

/// <summary>
/// Result of an assertion validation.
/// </summary>
/// <param name="IsValid">Whether the assertion is valid.</param>
/// <param name="UnknownKey">Whether the assertion is signed with a key not contained in the given keys.</param>
/// <param name="Claims">The claims of a valid assertion.</param>
public record AssertionResult(bool IsValid, bool UnknownKey, IDictionary<string, object> Claims)
{
  /// <summary>
  /// A failed validation.
  /// </summary>
  /// <param name="unknownKey">Whether the signing key is unknown.</param>
  /// <returns><see cref="AssertionResult"/></returns>
  public static AssertionResult Failed(bool unknownKey = false) => new(false, unknownKey, new Dictionary<string, object>());
}
