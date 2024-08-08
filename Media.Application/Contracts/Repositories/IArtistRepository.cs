using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IArtistRepository
{
  /// <summary>
  /// Loads list of artists.
  /// </summary>
  /// <param name="filter">Artists will be filtered by given filter</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Artists"/></returns>
  Artists List(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Loads a list of newest artists.
  /// </summary>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <returns><see cref="Artists"/></returns>
  Artists ListNewest(IEnumerable<Guid> clientGroups);

  /// <summary>
  /// Loads a list of most common artists.
  /// </summary>
  /// <param name="clientGroups">List of groups for which the records will be loaded.</param>
  /// <returns><see cref="Artists"/></returns>
  Artists ListCommon(IEnumerable<Guid> clientGroups);

  /// <summary>
  /// Tries to remove the given artist if there is no referenced record.
  /// </summary>
  /// <param name="artistName">Name of the artist to be removed.</param>
  Task TryRemove(string? artistName);

  /// <summary>
  /// Tries to load an artist or create a new one with the given name.
  /// </summary>
  /// <param name="artistName">Name of the artist to load or create.</param>
  /// <returns><see cref="Artist"/></returns>
  Task<Artist?> TryGetOrCreate(string? artistName);
}
