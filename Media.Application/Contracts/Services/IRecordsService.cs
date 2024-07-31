using System;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

interface IRecordsService {
  /// <summary>
  /// Deletes a <see cref="Models.Record"/> and if applicable the related
  /// Album, Artist and Genre.
  /// </summary>
  /// <param name="guid"></param>
  Task DeleteRecord(Guid guid);
}
