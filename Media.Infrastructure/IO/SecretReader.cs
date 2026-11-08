using System;
using System.IO;

namespace Media.Infrastructure.IO;

public static class SecretReader
{
  /// <summary>
  /// Reads a secret from the file in the environment variable <c>{name}_FILE</c>. If no file is configured or the file is empty,
  /// the value of the environment variable <c>{name}</c> is returned.
  /// </summary>
  /// <param name="name">Name of the environment variable of the secret.</param>
  /// <returns>The secret or null, if none is configured.</returns>
  /// <exception cref="InvalidOperationException">The configured file cannot be read.</exception>
  public static string? Read(string name)
  {
    var file = Environment.GetEnvironmentVariable($"{name}_FILE");
    if (!string.IsNullOrEmpty(file))
    {
      string secret;
      try
      {
        secret = File.ReadAllText(file).Trim();
      }
      catch (Exception e) when (e is IOException or UnauthorizedAccessException)
      {
        throw new InvalidOperationException($"The secret file '{file}' of {name}_FILE cannot be read.", e);
      }

      if (secret.Length > 0)
      {
        return secret;
      }
    }

    return Environment.GetEnvironmentVariable(name);
  }
}
