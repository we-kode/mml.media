namespace Media.Application.Models;

public class Info
{
  /// <summary>
  /// The path of the info.
  /// </summary>
  public string Path { get; set; } = string.Empty;

  /// <summary>
  /// The name of the directory or file.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// True, if the path is a folder and not a file.
  /// </summary>
  public bool IsFolder { get; set; }
}
