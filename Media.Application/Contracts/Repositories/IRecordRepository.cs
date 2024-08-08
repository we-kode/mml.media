using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IRecordRepository
{
  /// <summary>
  /// Stores the given metadata in index store.
  /// </summary>
  /// <param name="metaData"><see cref="RecordMetaData"/> to be stored.</param>
  /// <param name="groups">The groups of the record to be stored.</param>
  void SaveMetaData(RecordMetaData metaData, List<Guid> groups);

  /// <summary>
  /// Checks if one file is already indexed.
  /// </summary>
  /// <param name="checksum">Checksum of the file to be checked.</param>
  /// <returns>True, if index exists.</returns>
  bool IsIndexed(string checksum);

  /// <summary>
  /// Returns the given record as file stream.
  /// </summary>
  /// <param name="id">Id of record to be streamed.</param>
  /// <returns><see cref="Stream"/></returns>
  RecordStream StreamRecord(Guid id);

  /// <summary>
  /// Checks if one record has a group in given client groups.
  /// </summary>
  /// <param name="id">Id of the record to be checked.</param>
  /// <param name="clientGroups">List of groups.</param>
  /// <returns>True, if record is in one of the given groups.</returns>
  bool IsInGroup(Guid id, IEnumerable<Guid> clientGroups);

  /// <summary>
  /// Loads list of records.
  /// </summary>
  /// <param name="filter">Records will be filtered by given filter</param>
  /// <param name="tagFilter">Tags on which the records will be filtered.</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="groups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Records"/></returns>
  Records List(string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads list of record folders.
  /// </summary>
  /// <param name="filter">Records will be filtered by given filter</param>
  /// <param name="tagFilter">Tags on which the records will be filtered.</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="groups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Records"/></returns>
  RecordFolders ListFolder(string? filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip, int take);

  /// <summary>
  /// Lists the next record id in filtered list or null.
  /// </summary>
  /// <param name="id">Id of the actual record.</param>
  /// <param name="filter">Record title will be filtered by given filter.</param>
  /// <param name="tagFilter">Tags on which the records will be filtered.</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="repeat">If set, records will be loaded endless.</param>
  /// <param name="shuffle">If set, a random record id will be returned.</param>
  /// <returns><see cref="Guid"/> or null if no next record exists.</returns>
  Record? Next(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat, bool shuffle);

  /// <summary>
  /// Checks if on record exists.
  /// </summary>
  /// <param name="id">Id of record to be checked.</param>
  /// <returns>True if record exists.</returns>
  bool Exists(Guid id);

  /// <summary>
  /// Lists the previous record id in filtered list or null.
  /// </summary>
  /// <param name="id">Id of the actual record.</param>
  /// <param name="filter">Record title will be filtered by given filter.</param>
  /// <param name="tagFilter">Tags on which the records will be filtered.</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="repeat">If set, records will be loaded endless.</param>
  /// <returns><see cref="Guid"/> or null if no next record exists.</returns>
  Record? Previous(Guid id, string? filter, TagFilter tagFilter, bool filterByGroups, IEnumerable<Guid> clientGroups, bool repeat);

  /// <summary>
  /// Loads record.
  /// </summary>
  /// <param name="id">Id of record to be loaded.</param>
  /// <returns><see cref="Record"/></returns>
  Record GetRecord(Guid id);

  /// <summary>
  /// Tries to load record.
  /// </summary>
  /// <param name="id">Id of record to be loaded.</param>
  /// <returns><see cref="Record"/></returns>
  Record? TryGetRecord(Guid id);

  /// <summary>
  /// Updates one record.
  /// </summary>
  /// <param name="record"><see cref="Record"/> to be updated.</param>
  /// <param name="references">Reference guids to be set.</param>
  Task Update(Record record, (Guid? artistId, Guid? albumId, Guid? genreId, Guid? languageId) references);

  /// <summary>
  /// Remove the given record.
  /// </summary>
  /// <param name="recordId">Record that should be removed.</param>
  /// <returns></returns>
  Task RemoveRecord(Guid recordId);

  /// <summary>
  /// Returns physical file path of one record
  /// </summary>
  /// <param name="id">Record id</param>
  /// <returns>Path of record as string.</returns>
  string GetFilePath(Guid id);

  /// <summary>
  /// Loads records by checksums filtered by client groups.
  /// </summary>
  /// <param name="checksums">Checksums to be checked.</param>
  /// <param name="clientGroups">Groups the client is in.</param>
  /// <returns>List of Records.</returns>
  List<Record> GetRecords(List<string> checksums, IList<Guid> clientGroups);

  /// <summary>
  /// Loads records ids by folder.
  /// </summary>
  /// <param name="folder">Folder to load record ids for.</param>
  /// <returns>List of Records ids.</returns>
  List<Guid> GetRecords(RecordFolder folder);

  /// <summary>
  /// Assigns items to groups.
  /// </summary>
  /// <param name="items">Ids of items.</param>
  /// <param name="initGroups">The ids of the init selected groups.</param>
  /// <param name="groups">Ids of groups.</param>
  void Assign(List<Guid> items, List<Guid> initGroups, List<Guid> groups);

  /// <summary>
  /// Assigns items to groups.
  /// </summary>
  /// <param name="items">Items to be assigned.</param>
  /// <param name="initGroups">The ids of the init selected groups.</param>
  /// <param name="groups">Ids of groups.</param>
  void AssignFolder(IEnumerable<RecordFolder> items, List<Guid> initGroups, List<Guid> groups);

  /// <summary>
  /// Locks or unlocks records.
  /// </summary>
  /// <param name="items">Records to be locked or unlocked.</param>
  void Lock(List<Guid> items);

  /// <summary>
  /// Locks or unlocks items in folders.
  /// </summary>
  /// <param name="items">Folders to be locked or unlocked.</param>
  void LockFolder(IEnumerable<RecordFolder> items);

  /// <summary>
  /// Loads assigned groups of given items.
  /// </summary>
  /// <param name="items">Groups should be loaded.</param>
  /// <returns>Ids of assigned groups.</returns>
  Groups GetAssignedGroups(List<Guid> items);

  /// <summary>
  /// Loads assigned groups of given items.
  /// </summary>
  /// <param name="clients">Groups should be loaded.</param>
  /// <returns>Ids of assigned groups.</returns>
  Groups GetAssignedFolderGroups(IEnumerable<RecordFolder> folders);
}
