using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Media.Application.Contracts;

public interface IRecordsRepository
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
  /// <param name="id">Id of the rceord to be checked.</param>
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
  /// Loads list of albums.
  /// </summary>
  /// <param name="filter">Albums will be filtered by given filter</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Albums"/></returns>
  Albums ListAlbums(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);

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
  /// Loads list of artists.
  /// </summary>
  /// <param name="filter">Artists will be filtered by given filter</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Artists"/></returns>
  Artists ListArtists(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads list of genres.
  /// </summary>
  /// <param name="filter">Genres will be filtered by given filter</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Genres"/></returns>
  Genres ListGenres(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Removes one record.
  /// </summary>
  /// <param name="id">Id of record to be deleted.</param>
  Task DeleteRecord(Guid id);

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
  /// Upadtes one record.
  /// </summary>
  /// <param name="record"><see cref="Record"/> to be updated.</param>
  Task Update(Record record);

  /// <summary>
  /// Returns physical file path of one record
  /// </summary>
  /// <param name="id">Record id</param>
  /// <returns>Path of record as string.</returns>
  string GetFilePath(Guid id);

  /// <summary>
  /// Removes all records, which are in given folders.
  /// </summary>
  /// <param name="folders">Fodlers to delete.</param>
  Task DeleteFolders(IEnumerable<RecordFolder> folders);
}
