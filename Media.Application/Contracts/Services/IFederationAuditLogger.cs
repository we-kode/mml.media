using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Writes federation actions to the audit log and informs connected admins.
/// </summary>
public interface IFederationAuditLogger
{
  /// <summary>
  /// Logs one federation action. If the entry of a pending operation is given, this entry gets the result instead
  /// of adding a new one. A new entry is added if the given entry does not exist anymore.
  /// </summary>
  /// <param name="action">The performed action.</param>
  /// <param name="status">The result of the action.</param>
  /// <param name="source">The instance the action originated from.</param>
  /// <param name="target">The instance the action targeted.</param>
  /// <param name="transactionId">The id of the related transaction.</param>
  /// <param name="recordIds">The ids of the affected records.</param>
  /// <param name="message">Additional information or the error message.</param>
  /// <param name="entryId">The entry of the pending operation if available.</param>
  Task Log(AuditAction action, AuditStatus status, string source, string target, Guid? transactionId = null, IEnumerable<Guid>? recordIds = null, string? message = null, Guid? entryId = null);

  /// <summary>
  /// Informs connected admins about the entries of newly queued operations.
  /// </summary>
  /// <param name="entries">The pending entries.</param>
  Task Announce(IEnumerable<AuditEntry> entries);

  /// <summary>
  /// Marks the entry of an operation as skipped if it is still pending, e.g. because the operation was not
  /// necessary anymore.
  /// </summary>
  /// <param name="entryId">The entry of the operation if available.</param>
  /// <param name="message">The reason why the operation was skipped.</param>
  Task Resolve(Guid? entryId, string message);
}
