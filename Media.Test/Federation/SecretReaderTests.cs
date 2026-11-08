using Media.Infrastructure.IO;
using System;
using System.IO;
using Xunit;

namespace Media.Test.Federation;

public class SecretReaderTests
{
  [Fact]
  public void Read_PrefersTrimmedFileOverEnvironment()
  {
    var name = Name();
    var file = Path.GetTempFileName();
    File.WriteAllText(file, "from-file\n");
    Environment.SetEnvironmentVariable($"{name}_FILE", file);
    Environment.SetEnvironmentVariable(name, "from-env");

    Assert.Equal("from-file", SecretReader.Read(name));
  }

  [Fact]
  public void Read_UsesEnvironmentForEmptyFile()
  {
    var name = Name();
    var file = Path.GetTempFileName();
    Environment.SetEnvironmentVariable($"{name}_FILE", file);
    Environment.SetEnvironmentVariable(name, "from-env");

    Assert.Equal("from-env", SecretReader.Read(name));
  }

  [Fact]
  public void Read_UsesEnvironmentWithoutFile()
  {
    var name = Name();
    Environment.SetEnvironmentVariable(name, "from-env");

    Assert.Equal("from-env", SecretReader.Read(name));
  }

  [Fact]
  public void Read_ReturnsNullWithoutSecret()
  {
    Assert.Null(SecretReader.Read(Name()));
  }

  [Fact]
  public void Read_ThrowsForMissingFile()
  {
    var name = Name();
    Environment.SetEnvironmentVariable($"{name}_FILE", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

    var exception = Assert.Throws<InvalidOperationException>(() => SecretReader.Read(name));

    Assert.Contains($"{name}_FILE", exception.Message);
  }

  private static string Name() => $"MML_TEST_SECRET_{Guid.NewGuid():N}";
}
