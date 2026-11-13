using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Service which indexes uploaded files and extracts metadata from them. It also moves indexed files to a permanent location and deletes the original file from the temporary location.
/// </summary>
public interface IIndexService
{
  /// <summary>
  /// Indexes one file and extracts metadata from it. It also moves indexed files to a permanent location and deletes the original file from the temporary location.
  /// </summary>
  /// <param name="fileName">The name of the file.</param>
  /// <param name="modifiedAt">Last modification date of file.</param>
  /// <param name="groups">Group ids to which the file will be assigned to.</param>
  Task IndexFile(string fileName, DateTime modifiedAt, List<Guid> groups);
}
