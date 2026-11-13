using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Media.Infrastructure.Services;

/// <summary>
/// Generates and hashes the invitation codes partners present to request a registration.
/// </summary>
public static class FederationInvitationCode
{
  // Crockford base32 without ambiguous characters.
  private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
  private const int LENGTH = 20;
  private const int GROUP_SIZE = 5;

  /// <summary>
  /// Generates a new random code with 100 bits of entropy, formatted in groups like ABCDE-FGHJK-MNPQR-STVWX.
  /// </summary>
  /// <returns>The formatted code.</returns>
  public static string Generate()
  {
    var chars = Enumerable.Range(0, LENGTH).Select(_ => ALPHABET[RandomNumberGenerator.GetInt32(ALPHABET.Length)]).ToArray();
    return string.Join('-', Enumerable.Range(0, LENGTH / GROUP_SIZE).Select(i => new string(chars, i * GROUP_SIZE, GROUP_SIZE)));
  }

  /// <summary>
  /// Returns the hash of a code. Separators, whitespace and casing are ignored.
  /// </summary>
  /// <param name="code">The entered code.</param>
  /// <returns>The hex encoded SHA-256 hash or null if the code has an invalid format.</returns>
  public static string? Hash(string? code)
  {
    var normalized = new string([.. (code ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-').Select(char.ToUpperInvariant)]);
    if (normalized.Length != LENGTH || normalized.Any(c => !ALPHABET.Contains(c)))
    {
      return null;
    }

    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
  }
}
