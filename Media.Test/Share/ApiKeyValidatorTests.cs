using Media.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ApiKeyValidatorTests
{
  private const string AppKey = "app-key";

  [Theory]
  [InlineData("/media/s/open")]
  [InlineData("/media/s/download/token")]
  public async Task SharePages_AreOpenedWithoutAppKey(string path)
  {
    Assert.True(await Passes(path));
  }

  [Theory]
  [InlineData("/api/v2.0/media/record")]
  [InlineData("/share/open")]
  [InlineData("/s/open")]
  [InlineData("/search")]
  public async Task OtherPaths_NeedAppKey(string path)
  {
    Assert.False(await Passes(path));
    Assert.True(await Passes(path, AppKey));
  }

  private static async Task<bool> Passes(string path, string? key = null)
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?> { ["APP_KEY"] = AppKey, ["ADMIN_APP_KEY"] = "admin-key" })
      .Build();
    var passed = false;
    var validator = new ApiKeyValidator(_ =>
    {
      passed = true;
      return Task.CompletedTask;
    }, configuration);

    var context = new DefaultHttpContext();
    context.Request.Path = path;
    if (key != null)
    {
      context.Request.Headers["App-Key"] = key;
    }

    await validator.Invoke(context);
    return passed;
  }
}
