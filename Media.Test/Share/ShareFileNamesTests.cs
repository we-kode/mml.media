using Media.API.Share;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Media.Test.Share;

public class ShareFileNamesTests
{
  [Fact]
  public void Create_UsesTitleAsFileName()
  {
    Assert.Equal("Sermon 1.mp3", ShareFileNames.Create("Sermon 1", ".mp3", new HashSet<string>()));
  }

  [Theory]
  [InlineData("a/b\\c:d*e?f", "abcdef.mp3")]
  [InlineData("..", "record.mp3")]
  [InlineData("   ", "record.mp3")]
  [InlineData(null, "record.mp3")]
  public void Create_RemovesInvalidCharactersAndFallsBack(string? title, string expected)
  {
    Assert.Equal(expected, ShareFileNames.Create(title, ".mp3", new HashSet<string>()));
  }

  [Fact]
  public void Create_NumbersDuplicatesCaseInsensitive()
  {
    var used = new HashSet<string>();
    Assert.Equal("Song.mp3", ShareFileNames.Create("Song", ".mp3", used));
    Assert.Equal("song (2).mp3", ShareFileNames.Create("song", ".mp3", used));
    Assert.Equal("Song (3).mp3", ShareFileNames.Create("Song", ".mp3", used));
  }

  [Fact]
  public void Create_KeepsLongNames()
  {
    var name = new string('a', 200);
    Assert.Equal(name + ".mp3", ShareFileNames.Create(name, ".mp3", new HashSet<string>()));
  }

  [Fact]
  public void Create_ShortensNamesFileSystemsDoNotAllow()
  {
    var used = new HashSet<string>();
    var name = string.Concat(Enumerable.Repeat("ü👍", 100));

    var first = ShareFileNames.Create(name, ".mp3", used);
    var second = ShareFileNames.Create(name, ".mp3", used);

    Assert.True(Encoding.UTF8.GetByteCount(first) <= 255);
    Assert.True(Encoding.UTF8.GetByteCount(second) <= 255);
    Assert.EndsWith(" (2).mp3", second);
    Assert.DoesNotContain('�', Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(first)));
    Assert.StartsWith("ü👍ü👍", first);
  }

  [Theory]
  [InlineData("audio/mpeg", ".mp3")]
  [InlineData("audio/flac", ".flac")]
  [InlineData("unknown/type", ".mp3")]
  public void Extension_MapsMimeType(string mime, string expected)
  {
    Assert.Equal(expected, ShareFileNames.Extension(mime));
  }
}
