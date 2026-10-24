using Media.Application.Constants;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class FederationAssertionService(IFederationKeyStore keyStore, IMemoryCache cache) : IFederationAssertionService
{
  private const string REPLAY_CACHE_KEY = "MML_FEDERATION_JTI_";
  private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
  private static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(5);
  private static readonly Lock ReplayLock = new();
  private readonly JsonWebTokenHandler handler = new() { SetDefaultTimesOnTokenCreation = false };

  public string Create(string targetInstance, IDictionary<string, object>? claims = null)
  {
    var (keyId, rsa) = keyStore.GetSigningKey();
    var now = DateTime.UtcNow;
    var tokenClaims = new Dictionary<string, object>(claims ?? new Dictionary<string, object>())
    {
      [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
      [JwtRegisteredClaimNames.Sub] = Env.INSTANCE,
    };

    return handler.CreateToken(new SecurityTokenDescriptor
    {
      Issuer = Env.INSTANCE,
      Audience = FederationOptions.Audience(targetInstance),
      IssuedAt = now,
      NotBefore = now,
      Expires = now.Add(Lifetime),
      Claims = tokenClaims,
      SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = keyId }, SecurityAlgorithms.RsaSsaPssSha256),
    });
  }

  public string? ReadClaim(string assertion, string claim)
  {
    try
    {
      if (!handler.CanReadToken(assertion))
      {
        return null;
      }

      var token = handler.ReadJsonWebToken(assertion);
      return token.TryGetPayloadValue<object>(claim, out var value) ? value?.ToString() : null;
    }
    catch (ArgumentException)
    {
      return null;
    }
  }

  public async Task<AssertionResult> Validate(string assertion, string issuer, string? jwks)
  {
    if (string.IsNullOrWhiteSpace(jwks) || !handler.CanReadToken(assertion))
    {
      return AssertionResult.Failed(unknownKey: string.IsNullOrWhiteSpace(jwks));
    }

    JsonWebKeySet keySet;
    JsonWebToken unvalidated;
    try
    {
      keySet = new JsonWebKeySet(jwks);
      unvalidated = handler.ReadJsonWebToken(assertion);
    }
    catch (ArgumentException)
    {
      return AssertionResult.Failed();
    }

    if (string.IsNullOrEmpty(unvalidated.Kid) || !keySet.Keys.Any(k => k.Kid == unvalidated.Kid))
    {
      return AssertionResult.Failed(unknownKey: true);
    }

    var result = await handler.ValidateTokenAsync(assertion, new TokenValidationParameters
    {
      ValidIssuer = issuer,
      ValidAudience = FederationOptions.Audience(Env.INSTANCE),
      IssuerSigningKeys = keySet.GetSigningKeys(),
      ValidAlgorithms = [SecurityAlgorithms.RsaSsaPssSha256],
      RequireSignedTokens = true,
      RequireExpirationTime = true,
      ValidateLifetime = true,
      ClockSkew = TimeSpan.FromMinutes(1),
    }).ConfigureAwait(false);

    if (!result.IsValid || result.SecurityToken is not JsonWebToken token)
    {
      return AssertionResult.Failed();
    }

    if (token.ValidTo - token.ValidFrom > MaxLifetime || string.IsNullOrEmpty(token.Id))
    {
      return AssertionResult.Failed();
    }

    lock (ReplayLock)
    {
      var cacheKey = $"{REPLAY_CACHE_KEY}{issuer}_{token.Id}";
      if (cache.TryGetValue(cacheKey, out _))
      {
        return AssertionResult.Failed();
      }
      cache.Set(cacheKey, true, new DateTimeOffset(token.ValidTo).Add(TimeSpan.FromMinutes(2)));
    }

    return new AssertionResult(true, false, result.Claims);
  }
}
