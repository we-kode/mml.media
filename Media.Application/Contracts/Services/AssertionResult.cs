using System.Collections.Generic;

namespace Media.Application.Contracts.Services;

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
