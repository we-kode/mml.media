using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

public interface IRecordService {
  /// <summary>
  /// Deletes a <see cref="Models.Record"/> and if applicable the related
  /// Album, Artist and Genre.
  /// </summary>
  /// <param name="guid">Id of record to be deleted.</param>
  Task DeleteRecord(Guid guid);

  /// <summary>
  /// Removes all records, which are in given folders.
  /// </summary>
  /// <param name="folders">Folders to delete.</param>
  Task DeleteFolders(IEnumerable<RecordFolder> folders);

  /// <summary>
  /// Stores the given metadata in index store.
  /// </summary>
  /// <param name="metaData"><see cref="RecordMetaData"/> to be stored.</param>
  /// <param name="groups">The groups of the record to be stored.</param>
  Task SaveMetaData(RecordMetaData metaData, List<Guid> groups);

  /// <summary>
  /// Updates one record.
  /// </summary>
  /// <param name="record"><see cref="Record"/> to be updated.</param>
  Task Update(Record record);
}
