using Media.Application.Contracts.IO;
using Media.Application.Models.Federation;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;


namespace Media.Infrastructure.IO;

public class FederationKeyStore(FederationOptions options, string? secret) : IFederationKeyStore
{
  private const string SECRET_ENV = "FEDERATION_KEY_SECRET";
  private const string ENCRYPTED_PEM_LABEL = "ENCRYPTED PRIVATE KEY";
  private const int KEY_SIZE = 3072;
  private readonly Lock sync = new();
  private List<StoredKey>? keys;

  /// <summary>
  /// Initializes the key store, which encrypts the keys with the secret of the environment variable FEDERATION_KEY_SECRET if set.
  /// </summary>
  /// <param name="options">The federation options.</param>
  public FederationKeyStore(FederationOptions options) : this(options, Environment.GetEnvironmentVariable(SECRET_ENV))
  {
  }

  public bool Rotate(DateTime now)
  {
    lock (sync)
    {
      var stored = Load();
      var published = false;
      if (stored.Count == 0)
      {
        stored.Add(Create(now));
        published = true;
      }

      // a key is retired, when its successor is active for longer than the retire period.
      for (var i = stored.Count - 2; i >= 0; i--)
      {
        var successor = stored[i + 1];
        if (successor.ActivatesAt <= now && successor.ActivatesAt.AddDays(options.KeyRetireDays) <= now)
        {
          File.Delete(KeyPath(stored[i].KeyId));
          stored[i].Key.Dispose();
          stored.RemoveAt(i);
        }
      }

      var active = stored.LastOrDefault(k => k.ActivatesAt <= now) ?? stored[0];
      var hasPending = stored.Any(k => k.ActivatesAt > now);
      var rotateAt = active.ActivatesAt.AddDays(Math.Max(options.KeyRotationDays - options.KeyPublishLeadDays, 1));
      if (!hasPending && now >= rotateAt)
      {
        stored.Add(Create(now.AddDays(options.KeyPublishLeadDays)));
        published = true;
      }

      keys = stored;
      return published;
    }
  }

  public (string KeyId, RSA Key) GetSigningKey()
  {
    var now = DateTime.UtcNow;
    lock (sync)
    {
      if (keys == null || keys.Count == 0)
      {
        Rotate(now);
      }

      var active = keys!.LastOrDefault(k => k.ActivatesAt <= now) ?? keys![0];
      return (active.KeyId, active.Key);
    }
  }

  public string GetPublicJwks()
  {
    lock (sync)
    {
      if (keys == null || keys.Count == 0)
      {
        Rotate(DateTime.UtcNow);
      }

      var jwks = keys!.Select(k =>
      {
        var parameters = k.Key.ExportParameters(false);
        return new Dictionary<string, string>
        {
          ["kty"] = "RSA",
          ["use"] = "sig",
          ["alg"] = SecurityAlgorithms.RsaSsaPssSha256,
          ["kid"] = k.KeyId,
          ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
          ["e"] = Base64UrlEncoder.Encode(parameters.Exponent),
        };
      });
      return JsonSerializer.Serialize(new { keys = jwks });
    }
  }

  private List<StoredKey> Load()
  {
    if (keys != null)
    {
      return [.. keys.OrderBy(k => k.ActivatesAt)];
    }

    Directory.CreateDirectory(options.KeysPath);
    var loaded = new List<StoredKey>();
    foreach (var file in Directory.GetFiles(options.KeysPath, "*.json"))
    {
      var content = JsonSerializer.Deserialize<KeyFile>(File.ReadAllText(file));
      if (content == null)
      {
        continue;
      }

      var key = new StoredKey(content.KeyId, content.ActivatesAt, Import(content));
      if (HasSecret && !IsEncrypted(content.Key))
      {
        Save(key);
      }

      loaded.Add(key);
    }

    return [.. loaded.OrderBy(k => k.ActivatesAt)];
  }

  private StoredKey Create(DateTime activatesAt)
  {
    var key = new StoredKey(Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16)), activatesAt, RSA.Create(KEY_SIZE));
    Save(key);
    return key;
  }

  private RSA Import(KeyFile content)
  {
    var rsa = RSA.Create();
    if (!IsEncrypted(content.Key))
    {
      rsa.ImportFromPem(content.Key);
      return rsa;
    }

    if (!HasSecret)
    {
      throw new InvalidOperationException($"The federation key '{content.KeyId}' is encrypted, but {SECRET_ENV} is not set.");
    }

    try
    {
      rsa.ImportFromEncryptedPem(content.Key, secret);
      return rsa;
    }
    catch (CryptographicException e)
    {
      throw new InvalidOperationException($"The federation key '{content.KeyId}' cannot be decrypted with {SECRET_ENV}.", e);
    }
  }

  private void Save(StoredKey key)
  {
    var pem = HasSecret
      ? key.Key.ExportEncryptedPkcs8PrivateKeyPem(secret, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 600_000))
      : key.Key.ExportPkcs8PrivateKeyPem();

    Directory.CreateDirectory(options.KeysPath);
    var path = KeyPath(key.KeyId);
    var temporaryPath = $"{path}.tmp";
    File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new KeyFile(key.KeyId, key.ActivatesAt, pem)));
    if (!OperatingSystem.IsWindows())
    {
      File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    File.Move(temporaryPath, path, overwrite: true);
  }

  private bool HasSecret => !string.IsNullOrEmpty(secret);

  private static bool IsEncrypted(string pem) => pem.Contains(ENCRYPTED_PEM_LABEL, StringComparison.Ordinal);

  private string KeyPath(string keyId) => Path.Combine(options.KeysPath, $"{keyId}.json");

  private sealed record StoredKey(string KeyId, DateTime ActivatesAt, RSA Key);

  private sealed record KeyFile(string KeyId, DateTime ActivatesAt, string Key);
}
