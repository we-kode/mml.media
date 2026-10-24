using Media.Application.Models.Federation;
using Media.Infrastructure.IO;
using Media.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationAssertionServiceTests
{
  private static (FederationAssertionService Service, FederationKeyStore Store) Create()
  {
    var store = new FederationKeyStore(new FederationOptions { PublicUrl = "https://alpha.example", KeysPath = TestEnvironment.CreateTempFolder() });
    return (new FederationAssertionService(store, new MemoryCache(new MemoryCacheOptions())), store);
  }

  [Fact]
  public async Task Validate_AcceptsAssertionOnlyOnce()
  {
    var (service, store) = Create();
    var assertion = service.Create(TestEnvironment.Instance, new Dictionary<string, object> { ["request_id"] = "42" });

    var result = await service.Validate(assertion, TestEnvironment.Instance, store.GetPublicJwks());
    Assert.True(result.IsValid);
    Assert.Equal("42", result.Claims["request_id"].ToString());

    var replay = await service.Validate(assertion, TestEnvironment.Instance, store.GetPublicJwks());
    Assert.False(replay.IsValid);
  }

  [Fact]
  public async Task Validate_RejectsAssertionForOtherInstance()
  {
    var (service, store) = Create();
    var assertion = service.Create("beta");

    var result = await service.Validate(assertion, TestEnvironment.Instance, store.GetPublicJwks());

    Assert.False(result.IsValid);
    Assert.False(result.UnknownKey);
  }

  [Fact]
  public async Task Validate_RejectsUnexpectedIssuer()
  {
    var (service, store) = Create();
    var assertion = service.Create(TestEnvironment.Instance);

    var result = await service.Validate(assertion, "beta", store.GetPublicJwks());

    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task Validate_ReportsUnknownKey()
  {
    var (service, _) = Create();
    var (_, otherStore) = Create();
    var assertion = service.Create(TestEnvironment.Instance);

    var result = await service.Validate(assertion, TestEnvironment.Instance, otherStore.GetPublicJwks());

    Assert.False(result.IsValid);
    Assert.True(result.UnknownKey);
  }

  [Fact]
  public async Task Validate_RejectsManipulatedAssertion()
  {
    var (service, store) = Create();
    var assertion = service.Create(TestEnvironment.Instance);
    var parts = assertion.Split('.');
    var manipulated = $"{parts[0]}.{parts[1]}x.{parts[2]}";

    var result = await service.Validate(manipulated, TestEnvironment.Instance, store.GetPublicJwks());

    Assert.False(result.IsValid);
  }

  [Fact]
  public void ReadClaim_ReturnsUnvalidatedClaim()
  {
    var (service, _) = Create();
    var assertion = service.Create("beta", new Dictionary<string, object> { ["base_url"] = "https://alpha.example" });

    Assert.Equal(TestEnvironment.Instance, service.ReadClaim(assertion, "iss"));
    Assert.Equal("https://alpha.example", service.ReadClaim(assertion, "base_url"));
    Assert.Null(service.ReadClaim("no-token", "iss"));
  }
}
