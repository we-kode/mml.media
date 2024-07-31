using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IGenresRepository
{
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
  /// Determines whether genre exists.
  /// </summary>
  /// <param name="genreId">Id of genre.</param>
  /// <returns>True, if genre exists.</returns>
  bool GenreExists(Guid genreId);

  /// <summary>
  /// Load all saved bitrates for compressions.
  /// </summary>
  /// <returns><see cref="GenreBitrates"/></returns>
  GenreBitrates Bitrates();

  /// <summary>
  /// Removes bitrate from genre.
  /// </summary>
  /// <param name="genreId">Id of genre, the bitrate should be deleted.</param>
  void DeleteBitrate(Guid genreId);

  /// <summary>
  /// Updates or create bitrates for genres.
  /// </summary>
  /// <param name="bitrates">Bitrates to be updated.</param>
  void UpdateBitrates(List<GenreBitrate> bitrates);

  /// <summary>
  /// Updates the bitrate index of one record.
  /// </summary>
  /// <param name="checksum">The checksum of record to be updated.</param>
  /// <param name="bitrate">The new bitrate to be set.</param>
  Task UpdateBitrate(string checksum, int bitrate);

  /// <summary>
  /// Returns bitrate by genre name or null if no bitrate exists.
  /// </summary>
  /// <param name="genreName">Name of genre.</param>
  /// <returns><see cref="int?"/></returns>
  int? Bitrate(string genreName);
}
