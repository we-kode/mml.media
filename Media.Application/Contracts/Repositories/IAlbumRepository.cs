using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IAlbumRepository
{
  /// <summary>
  /// Loads list of albums.
  /// </summary>
  /// <param name="filter">Albums will be filtered by given filter</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Albums"/></returns>
  Albums List(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Tries to remove the given album if there is no referenced record.
  /// </summary>
  /// <param name="albumName">Name of the album to be removed.</param>
  Task TryRemove(string? albumName);

  /// <summary>
  /// Tries to load an album or create a new one with the given name.
  /// </summary>
  /// <param name="albumName">Name of the album to load or create.</param>
  /// <returns><see cref="Album"/></returns>
  Task<Album?> TryGetOrCreate(string? albumName);
}