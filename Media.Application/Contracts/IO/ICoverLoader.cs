using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.IO;

/// <summary>
/// Handles cover save load operations
/// </summary>
public interface ICoverLoader
{
  /// <summary>
  /// Saves the given cover data to the file system. The file name is generated based on the content of the cover. This is used to avoid storing duplicate covers and to be able to easily retrieve the cover file based on the content.
  /// </summary>
  /// <param name="data">The data to save</param>
  /// <param name="fileName">The filename on disk</param>
  /// <returns></returns>
  Task Save(byte[] data, string fileName);

  /// <summary>
  /// Loads the cover data from the file system based on the given file name. The file name is generated based on the content of the cover. This is used to avoid storing duplicate covers and to be able to easily retrieve the cover file based on the content.
  /// </summary>
  /// <param name="fileName">File to load.</param>
  /// <returns>The byte data of the file or null if file not exists.</returns>
  Task<byte[]?> Load(string fileName);

  /// <summary>
  /// Deletes the cover file with the given name if it exists.
  /// </summary>
  /// <param name="fileName">File to delete.</param>
  void Delete(string fileName);

  /// <summary>
  /// Returns the names of all stored cover files, which were last written before the given time.
  /// </summary>
  /// <param name="writtenBefore">Only files written before this time are returned.</param>
  /// <returns>The file names.</returns>
  IEnumerable<string> List(DateTime writtenBefore);
}
