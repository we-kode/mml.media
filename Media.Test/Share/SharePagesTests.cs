using Media.API.Extensions;
using Media.API.Share;
using Media.Application.Models.Options;
using System;
using System.IO;
using Xunit;

namespace Media.Test.Share;

public class SharePagesTests : IDisposable
{
  private readonly string folder = TestEnvironment.CreateTempFolder();

  public void Dispose()
  {
    Directory.Delete(folder, true);
    GC.SuppressFinalize(this);
  }

  private ShareStyling Load(string json)
  {
    var path = Path.Combine(folder, "styling.json");
    File.WriteAllText(path, json);
    return new ShareOptions { StylingPath = path }.LoadShareStyling();
  }

  [Fact]
  public void Default_UsesTitleAndColorsOfTheDefaultApp()
  {
    var page = new SharePages(new ShareStyling()).Download(3, true, new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));

    Assert.Contains("--primary:#245FA6;", page);
    Assert.Contains("--primary:#A8C8FF;", page);
    Assert.Contains("<title>Meine Mediathek</title>", page);
  }

  [Fact]
  public void MissingFile_UsesTheDefaults()
  {
    var page = new SharePages(new ShareOptions { StylingPath = Path.Combine(folder, "missing.json") }.LoadShareStyling()).Invalid();

    Assert.Contains("--primary:#245FA6;", page);
    Assert.Contains("<title>Meine Mediathek</title>", page);
  }

  [Theory]
  [InlineData("")]
  [InlineData("{ not json")]
  [InlineData("[1, 2]")]
  [InlineData("\"text\"")]
  public void UnreadableFile_UsesTheDefaults(string content)
  {
    var page = new SharePages(Load(content)).Download(1, true, DateTime.UtcNow);

    Assert.Contains("--primary:#245FA6;", page);
    Assert.Contains("<title>Meine Mediathek</title>", page);
  }

  [Fact]
  public void StylingFile_ReplacesTitleAndColors()
  {
    var appearance = Load("""
      {
        "title": { "default": "Chor Archiv", "en": "Choir Archive" },
        "colors": { "light": { "primary": "#112233" }, "dark": { "Primary": "#AABBCC" } }
      }
      """);

    var page = new SharePages(appearance).Open();

    Assert.Contains("<title>Chor Archiv</title>", page);
    Assert.Contains("\"en\":\"Choir Archive\"", page);
    Assert.Contains("\"de\":\"Meine Mediathek\"", page);
    Assert.Contains("--primary:#112233;", page);
    Assert.Contains("--primary:#AABBCC;", page);
    Assert.Contains("--surface:#FDFBFF;", page);
  }

  [Theory]
  [InlineData("\"red\"")]
  [InlineData("\"#12345\"")]
  [InlineData("\"#112233;}</style><script>alert(1)</script>\"")]
  [InlineData("12")]
  public void StylingFile_IgnoresColorsWhichAreNoHexValues(string color)
  {
    var page = new SharePages(Load($$"""{ "colors": { "light": { "primary": {{color}} } } }""")).Download(1, true, DateTime.UtcNow);

    Assert.Contains("--primary:#245FA6;", page);
    Assert.DoesNotContain("alert(1)", page);
  }

  [Fact]
  public void StylingFile_TitlesAreEncoded()
  {
    var page = new SharePages(Load("""{ "title": { "default": "<b>Archive</b>", "de": "</script><i>" } }""")).Invalid();

    Assert.DoesNotContain("<b>Archive</b>", page);
    Assert.DoesNotContain("</script><i>", page);
    Assert.Contains("&lt;b&gt;Archive&lt;/b&gt;", page);
  }

  [Fact]
  public void Pages_ContainTheCountOnly()
  {
    var page = new SharePages(new ShareStyling()).Download(7, false, DateTime.UtcNow);

    Assert.Contains("data-n=\"7\"", page);
    Assert.Contains("Recordings: 7", page);
  }

  [Theory]
  [InlineData(true, "downloadSingleUse")]
  [InlineData(false, "downloadMultiUse")]
  public void DownloadPage_ShowsSingleUseAndExpiry(bool singleUse, string text)
  {
    var page = new SharePages(new ShareStyling()).Download(2, singleUse, new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));

    Assert.Contains($"data-i=\"{text}\"", page);
    Assert.Contains("data-exp=\"2030-01-02T03:04:05Z\"", page);
  }
}
