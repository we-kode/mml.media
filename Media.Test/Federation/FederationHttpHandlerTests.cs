using Media.Application.Models.Federation;
using Media.Infrastructure.Services;
using System.Net;
using Xunit;

namespace Media.Test.Federation;

public class FederationHttpHandlerTests
{
  [Theory]
  [InlineData("127.0.0.1", true)]
  [InlineData("10.1.2.3", true)]
  [InlineData("172.16.0.1", true)]
  [InlineData("172.32.0.1", false)]
  [InlineData("192.168.1.10", true)]
  [InlineData("169.254.169.254", true)]
  [InlineData("100.64.0.1", true)]
  [InlineData("0.0.0.0", true)]
  [InlineData("::1", true)]
  [InlineData("fe80::1", true)]
  [InlineData("fd00::1", true)]
  [InlineData("::ffff:192.168.1.1", true)]
  [InlineData("8.8.8.8", false)]
  [InlineData("2001:4860:4860::8888", false)]
  public void IsPrivate_DetectsNonPublicAddresses(string address, bool expected)
  {
    Assert.Equal(expected, FederationHttpHandler.IsPrivate(IPAddress.Parse(address)));
  }

  [Theory]
  [InlineData("http://beta.example")]
  [InlineData("ftp://beta.example")]
  [InlineData("https://user:pwd@beta.example")]
  [InlineData("https://beta.example/?redirect=x")]
  [InlineData("beta.example")]
  [InlineData("")]
  public void ValidateUrl_RejectsInvalidUrls(string url)
  {
    var exception = Assert.Throws<FederationException>(() => FederationHttpHandler.ValidateUrl(url));
    Assert.Equal(FederationErrors.InvalidUrl, exception.Code);
  }

  [Fact]
  public void ValidateUrl_AcceptsHttpsUrl()
  {
    Assert.Equal("beta.example", FederationHttpHandler.ValidateUrl("https://beta.example:5050").Host);
  }
}
