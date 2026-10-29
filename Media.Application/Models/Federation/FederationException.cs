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
