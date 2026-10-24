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
  /// <param name="removeLocked">Deletes the record even if it is locked.</param>
  Task DeleteRecord(Guid guid, bool removeLocked = false);

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
  /// Updates one record. Records owned by another instance only get groups and lock updated.
  /// Changed metadata of an own record are sent to all partners the record was synchronized with.
  /// </summary>
  /// <param name="record"><see cref="Record"/> to be updated.</param>
  /// <param name="cover">The cover of the record as byte array.</param>
  Task Update(Record record, byte[] cover);

  /// <summary>
  /// Updates the metadata of a record with the changes sent by its owner. Groups and lock are kept.
  /// </summary>
  /// <param name="record"><see cref="Record"/> with the new metadata.</param>
  /// <param name="cover">The cover of the record as byte array.</param>
  Task UpdateFromOwner(Record record, byte[] cover);
}
