using Media.Infrastructure.Services;
using System.Linq;
using Xunit;

namespace Media.Test.Federation;

public class FederationInvitationCodeTests
{
  [Fact]
  public void Generate_CreatesUniqueFormattedCodes()
  {
    var codes = Enumerable.Range(0, 100).Select(_ => FederationInvitationCode.Generate()).ToList();

    Assert.Equal(100, codes.Distinct().Count());
    Assert.All(codes, code => Assert.Matches("^[0-9A-Z]{5}(-[0-9A-Z]{5}){3}$", code));
  }

  [Fact]
  public void Hash_IgnoresSeparatorsWhitespaceAndCasing()
  {
    var code = FederationInvitationCode.Generate();

    Assert.Equal(FederationInvitationCode.Hash(code), FederationInvitationCode.Hash($" {code.Replace("-", " ").ToLowerInvariant()} "));
    Assert.NotEqual(FederationInvitationCode.Hash(code), FederationInvitationCode.Hash(FederationInvitationCode.Generate()));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("ABCDE-FGHJK-MNPQR")]
  [InlineData("ABCDE-FGHJK-MNPQR-STVWXY")]
  [InlineData("ABCDE-FGHJK-MNPQR-STVWU")]
  public void Hash_RejectsInvalidCodes(string? code)
  {
    Assert.Null(FederationInvitationCode.Hash(code));
  }
}
