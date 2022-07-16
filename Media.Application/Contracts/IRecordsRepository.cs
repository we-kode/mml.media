using Media.Application.Models;
using System;
using System.Collections.Generic;

namespace Media.Application.Contracts;

public interface IRecordsRepository
{
  /// <summary>
  /// Stores the given metadata in index store.
  /// </summary>
  /// <param name="metaData"><see cref="RecordMetaData"/> to be stored.</param>
  public void SaveMetaData(RecordMetaData metaData);

  /// <summary>
  /// Checks if one file is already indexed.
  /// </summary>
  /// <param name="checksum">Checksum of the file to be checked.</param>
  /// <returns>True, if index exists.</returns>
  bool IsIndexed(string checksum);

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
  Records List(string filter, TagFilter tagFilter, bool filterByGroups, IList<Guid> groups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads list of albums.
  /// </summary>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Albums"/></returns>
  Albums ListAlbums(int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads list of artists.
  /// </summary>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Artists"/></returns>
  Artists ListArtists(int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads list of genres.
  /// </summary>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Genres"/></returns>
  Genres ListGenres(int skip = Constants.List.Skip, int take = Constants.List.Take);
}
