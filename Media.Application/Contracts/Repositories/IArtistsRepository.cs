using Media.Application.Models;
using System;
using System.Collections.Generic;

namespace Media.Application.Contracts.Repositories;

public interface IArtistsRepository
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
  Artists ListArtists(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = Constants.List.Skip, int take = Constants.List.Take);
}
