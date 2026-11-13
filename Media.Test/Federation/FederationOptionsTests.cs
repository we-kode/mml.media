using Media.Application.Models.Federation;
using System;
using Xunit;

namespace Media.Test.Federation;

public class FederationOptionsTests
{
  [Theory]
  [InlineData("alpha")]
  [InlineData("kst.dev_01-a")]
  public void EnsureValidInstance_AcceptsValidName(string instance)
  {
    new FederationOptions { PublicUrl = "https://alpha.example" }.EnsureValidInstance(instance);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("my instance")]
  [InlineData("o'brien")]
  public void EnsureValidInstance_RejectsInvalidNameIfEnabled(string? instance)
  {
    Assert.Throws<InvalidOperationException>(() => new FederationOptions { PublicUrl = "https://alpha.example" }.EnsureValidInstance(instance));
  }

  [Fact]
  public void EnsureValidInstance_IgnoresNameIfDisabled()
  {
    new FederationOptions().EnsureValidInstance("my instance");
  }

  [Theory]
  [InlineData("7", 7)]
  [InlineData("365", 365)]
  [InlineData("0", 0)]
  [InlineData("45", AuditRetention.DefaultDays)]
  [InlineData("", AuditRetention.DefaultDays)]
  [InlineData(null, AuditRetention.DefaultDays)]
  public void AuditRetention_AcceptsSelectableValuesOnly(string? value, int expected)
  {
    Assert.Equal(expected, AuditRetention.Days(value));
  }

  [Fact]
  public void UseIssuerAsDefault_UsesIssuerWithoutTrailingSlash()
  {
    var options = new FederationOptions().UseIssuerAsDefault("https://alpha.example:5050/");

    Assert.True(options.IsEnabled);
    Assert.Equal("https://alpha.example:5050", options.PublicUrl);
    Assert.Equal("https://alpha.example:5050/api/v2.0/media/federation/peer", options.Endpoint);
  }

  [Fact]
  public void UseIssuerAsDefault_KeepsConfiguredPublicUrl()
  {
    var options = new FederationOptions { PublicUrl = "https://public.example" }.UseIssuerAsDefault("https://intern.example/");

    Assert.Equal("https://public.example", options.PublicUrl);
  }
}
