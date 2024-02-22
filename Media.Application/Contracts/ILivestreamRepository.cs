using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Media.Application.Contracts;

public interface ILivestreamRepository
{
  /// <summary>
  /// Assigns items to groups.
  /// </summary>
  /// <param name="items">Ids of items.</param>
  /// <param name="initGroups">The ids of the init selected groups.</param>
  /// <param name="groups">Ids of groups.</param>
  void Assign(List<Guid> items, List<Guid> initGroups, List<Guid> groups);

  /// <summary>
  /// Deletes one livestream.
  /// </summary>
  /// <param name="id">id of stream to be deleted.</param>
  Task Delete(Guid id);

  /// <summary>
  /// Checks if one stream exists.
  /// </summary>
  /// <param name="id">Id of stream to be checked.</param>
  /// <returns>True, if exists.</returns>
  bool Exists(Guid id);

  /// <summary>
  /// Loads assinged groups of given items.
  /// </summary>
  /// <param name="items">Groups should be laoded.</param>
  /// <returns>Ids of assigned groups.</returns>
  Groups GetAssignedGroups(List<Guid> items);

  /// <summary>
  /// Checks, if user is in group with the gievn stream.
  /// </summary>
  /// <param name="id">The id of the stream</param>
  /// <param name="clientGroups">Groups of the user.</param>
  /// <returns>True, if the user is in one group with the stream.</returns>
  bool IsInGroup(Guid id, IList<Guid> clientGroups);

  /// <summary>
  /// Loads list of livestreams.
  /// </summary>
  /// <param name="filter">Streams will be filtered by given filter.</param>
  /// <param name="filterByGroups">True if records will be filtered by groups.</param>
  /// <param name="clientGroups">List of groups for which the stream will be loaded.</param>
  /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
  /// <param name="take">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
  /// <returns><see cref="Livestreams"/></returns>
  Livestreams List(string? filter, bool filterByGroups, IList<Guid> clientGroups, int skip, int take);
  
  /// <summary>
  /// Loads settings of one livestream.
  /// </summary>
  /// <param name="id">Id of stream to be loaded.</param>
  /// <returns><see cref="LivestreamSettings"/></returns>
  LivestreamSettings Load(Guid id);
  
  /// <summary>
  /// Loads the livestream and streams it.
  /// </summary>
  /// <param name="id">Id of the livestream to be loaded.</param>
  /// <returns>Stream of the Livestream endpoint</returns>
  string Stream(Guid id);

  /// <summary>
  /// Updates or creates a stream entry.
  /// </summary>
  /// <param name="value">Settings to be stored.</param>
  Task Update(LivestreamSettings value);
}
