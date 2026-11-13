using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Media.Test;

internal static class TestEnvironment
{
  public const string Instance = "alpha";

  [ModuleInitializer]
  internal static void Initialize()
  {
    Environment.SetEnvironmentVariable("INSTANCE", Instance);
  }

  public static string CreateTempFolder()
  {
    var path = Path.Combine(Path.GetTempPath(), "mml-media-test", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
  }
}
