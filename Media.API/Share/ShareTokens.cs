using System;
using System.Security.Cryptography;
using System.Text;

namespace Media.API.Share;

/// <summary>
/// Creates secret tokens of share links and the hashes stored for them.
/// </summary>
public static class ShareTokens
{
  /// <summary>
  /// Creates a new random token.
  /// </summary>
  /// <param name="bytes">Number of random bytes of the token. The default has 256 bit of entropy.</param>
  /// <returns>URL safe token.</returns>
  public static string Create(int bytes = 32)
  {
    return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  /// <summary>
  /// Hashes a token, so the secret itself is never stored.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <returns>SHA-256 hash as hex string.</returns>
  public static string Hash(string token)
  {
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
  }
}
