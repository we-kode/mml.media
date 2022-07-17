using Media.Application.Models;
using System.Threading.Tasks;

namespace Media.Application.Contracts;

public interface IRecordsRepository
{
  /// <summary>
  /// Stores the given metadata in index store.
  /// </summary>
  /// <param name="metaData"><see cref="RecordMetaData"/> to be stored.</param>
  Task SaveMetaData(RecordMetaData metaData);

  /// <summary>
  /// Checks if one file is already indexed.
  /// </summary>
  /// <param name="checksum">Checksum of the file to be checked.</param>
  /// <returns>True, if index exists.</returns>
  bool IsIndexed(string checksum);
}
