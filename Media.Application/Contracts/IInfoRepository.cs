using Media.Application.Models;

namespace Media.Application.Contracts;

public interface IInfoRepository
{

  /// <summary>
  /// Returns content of file
  /// </summary>
  /// <param name="path">Path fo file.</param>
  /// <returns>Content of file.</returns>
  string Content(string path);

  /// <summary>
  /// Determines, whether folder exists.
  /// </summary>
  /// <param name="path">Path to be checked.</param>
  /// <returns>True, if path exists.</returns>
  bool ExistsFolder(string path);

  /// <summary>
  /// Determines, whether file exists.
  /// </summary>
  /// <param name="path">Path to be checked.</param>
  /// <returns>True, if path exists.</returns>
  bool ExistsFile(string path);

  /// <summary>
  /// Lists all files and folders of one path.
  /// </summary>
  /// <param name="path">Path to be listed.</param>
  /// <returns><see cref="Infos"></returns>
  Infos List(string? path);
}
