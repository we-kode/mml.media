using Media.API.Share;
using Xunit;

namespace Media.Test.Share;

public class ShareTokensTests
{
  [Fact]
  public void Create_ReturnsUniqueUrlSafeTokens()
  {
    var a = ShareTokens.Create();
    var b = ShareTokens.Create();

    Assert.NotEqual(a, b);
    Assert.Equal(43, a.Length);
    Assert.DoesNotContain(a, c => c == '+' || c == '/' || c == '=');
  }

  [Fact]
  public void Hash_IsStableAndDoesNotContainToken()
  {
    var token = ShareTokens.Create();

    Assert.Equal(ShareTokens.Hash(token), ShareTokens.Hash(token));
    Assert.Equal(64, ShareTokens.Hash(token).Length);
    Assert.DoesNotContain(token, ShareTokens.Hash(token));
  }
}
