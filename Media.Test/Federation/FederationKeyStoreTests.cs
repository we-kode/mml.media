using Media.Application.Models.Federation;
using Media.Infrastructure.IO;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Media.Test.Federation;

public class FederationKeyStoreTests
{
  private static FederationOptions Options(string path) => new()
  {
    PublicUrl = "https://alpha.example",
    KeysPath = path,
    KeyRotationDays = 90,
    KeyPublishLeadDays = 7,
    KeyRetireDays = 14,
  };

  private static string[] KeyIds(FederationKeyStore store) => [.. new JsonWebKeySet(store.GetPublicJwks()).Keys.Select(k => k.Kid)];

  [Fact]
  public void Rotate_PublishesKeyBeforeUseAndRetiresReplacedKey()
  {
    var store = new FederationKeyStore(Options(TestEnvironment.CreateTempFolder()));
    var start = DateTime.UtcNow.AddDays(-100);

    Assert.True(store.Rotate(start));
    var firstKey = Assert.Single(KeyIds(store));

    Assert.False(store.Rotate(start.AddDays(82)));
    Assert.Single(KeyIds(store));

    // the new key is published 7 days before it is activated
    Assert.True(store.Rotate(start.AddDays(83)));
    var keys = KeyIds(store);
    Assert.Equal(2, keys.Length);
    var secondKey = Assert.Single(keys, k => k != firstKey);

    // now is 100 days after start, so the second key is active since 10 days
    Assert.Equal(secondKey, store.GetSigningKey().KeyId);

    Assert.False(store.Rotate(start.AddDays(103)));
    Assert.Equal(2, KeyIds(store).Length);

    Assert.False(store.Rotate(start.AddDays(104)));
    Assert.Equal(secondKey, Assert.Single(KeyIds(store)));
  }

  [Fact]
  public void Rotate_KeepsKeysAcrossRestarts()
  {
    var path = TestEnvironment.CreateTempFolder();
    var first = new FederationKeyStore(Options(path));
    first.Rotate(DateTime.UtcNow);
    var keyId = first.GetSigningKey().KeyId;

    var restarted = new FederationKeyStore(Options(path));
    Assert.False(restarted.Rotate(DateTime.UtcNow));
    Assert.Equal(keyId, restarted.GetSigningKey().KeyId);
  }

  [Fact]
  public void GetPublicJwks_ContainsNoPrivateKeyParameters()
  {
    var store = new FederationKeyStore(Options(TestEnvironment.CreateTempFolder()));
    var jwks = store.GetPublicJwks();

    var key = Assert.Single(new JsonWebKeySet(jwks).Keys);
    Assert.Equal("RSA", key.Kty);
    Assert.Null(key.D);
    Assert.Null(key.P);
    Assert.Null(key.Q);
  }

  private static string KeyFile(string path) => Assert.Single(Directory.GetFiles(path, "*.json"));

  [Fact]
  public void Rotate_EncryptsStoredKeysWhenSecretIsSetLater()
  {
    var path = TestEnvironment.CreateTempFolder();
    var plain = new FederationKeyStore(Options(path), null);
    plain.Rotate(DateTime.UtcNow);
    var keyId = plain.GetSigningKey().KeyId;
    Assert.DoesNotContain("ENCRYPTED", File.ReadAllText(KeyFile(path)));

    var encrypted = new FederationKeyStore(Options(path), "secret");
    Assert.False(encrypted.Rotate(DateTime.UtcNow));

    Assert.Equal(keyId, encrypted.GetSigningKey().KeyId);
    Assert.Contains("ENCRYPTED PRIVATE KEY", File.ReadAllText(KeyFile(path)));
    Assert.Equal(keyId, new FederationKeyStore(Options(path), "secret").GetSigningKey().KeyId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("other")]
  public void Rotate_FailsForEncryptedKeyWithoutMatchingSecret(string? secret)
  {
    var path = TestEnvironment.CreateTempFolder();
    new FederationKeyStore(Options(path), "secret").Rotate(DateTime.UtcNow);
    var content = File.ReadAllText(KeyFile(path));

    var exception = Assert.Throws<InvalidOperationException>(() => new FederationKeyStore(Options(path), secret).Rotate(DateTime.UtcNow));

    Assert.Contains("FEDERATION_KEY_SECRET", exception.Message);
    Assert.Equal(content, File.ReadAllText(KeyFile(path)));
  }
}
