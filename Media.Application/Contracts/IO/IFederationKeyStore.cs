using System;
using System.Security.Cryptography;

namespace Media.Application.Contracts.IO;

/// <summary>
/// Stores the keys this instance signs federation requests with and rotates them.
/// </summary>
public interface IFederationKeyStore
{
  /// <summary>
  /// Creates, activates and retires keys according to the rotation configuration.
  /// </summary>
  /// <param name="now">The current date.</param>
  /// <returns>True, if a new key was published and partners need to be informed.</returns>
  bool Rotate(DateTime now);

  /// <summary>
  /// Returns the key currently used for signing.
  /// </summary>
  /// <returns>Id and RSA key of the active key.</returns>
  (string KeyId, RSA Key) GetSigningKey();

  /// <summary>
  /// Returns all published public keys as JSON web key set.
  /// </summary>
  /// <returns>The JSON web key set.</returns>
  string GetPublicJwks();
}
