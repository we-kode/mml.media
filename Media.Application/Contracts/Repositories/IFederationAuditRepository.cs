using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationAuditRepository
{
  /// <summary>
  /// Returns a list of audit log entries matching the given query in its sort order.
  /// </summary>
  /// <param name="query">Filter and sort order of the entries.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="AuditEntries"/></returns>
  AuditEntries List(AuditQuery query, int skip = Constants.List.Skip, int take = Constants.List.Take);

  /// <summary>
  /// Returns the names of all partner instances occurring in the audit log.
  /// </summary>
  /// <returns>The instance names in alphabetical order.</returns>
  IList<string> ListInstances();

  /// <summary>
  /// Adds one entry to the audit log.
  /// </summary>
  /// <param name="entry">Entry to be added.</param>
  /// <returns>The stored <see cref="AuditEntry"/>.</returns>
  Task<AuditEntry> Add(AuditEntry entry);

  /// <summary>
  /// Changes the status and the message of an entry.
  /// </summary>
  /// <param name="entryId">Id of the entry.</param>
  /// <param name="status">The new status.</param>
  /// <param name="message">The new message.</param>
  /// <param name="onlyPending">Changes the entry only if it is pending.</param>
  /// <returns>The changed entry and its previous status or null if the entry does not exist or was not changed.</returns>
  Task<(AuditEntry Entry, AuditStatus Previous)?> Update(Guid entryId, AuditStatus status, string? message, bool onlyPending = false);

  /// <summary>
  /// Removes the given entries.
  /// </summary>
  /// <param name="entryIds">Ids of the entries.</param>
  Task Delete(IEnumerable<Guid> entryIds);

  /// <summary>
  /// Removes all entries matching the given query.
  /// </summary>
  /// <param name="query">Filter of the entries. The sort order is ignored.</param>
  /// <returns>The count of removed entries.</returns>
  Task<int> Delete(AuditQuery query);

  /// <summary>
  /// Removes all entries created before the given date.
  /// </summary>
  /// <param name="before">Entries older than this date are removed.</param>
  /// <returns>The count of removed entries.</returns>
  Task<int> DeleteOlderThan(DateTime before);
}
