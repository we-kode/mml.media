using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Repositories;

public interface IFederationJobRepository
{
  /// <summary>
  /// Queues a new operation which is executed as soon as possible.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="type">Type of the operation.</param>
  /// <param name="recordId">Id of the related local record if available.</param>
  /// <param name="transactionId">Id of the related transaction if available.</param>
  Task Enqueue(Guid peerId, FederationJobType type, Guid? recordId = null, Guid? transactionId = null);

  /// <summary>
  /// Queues an operation for every partner the local record was sent to. An update is only queued once per partner and record.
  /// </summary>
  /// <param name="recordId">Id of the local record.</param>
  /// <param name="type">Type of the operation.</param>
  /// <returns>The pending audit log entries of the queued operations.</returns>
  Task<IList<AuditEntry>> EnqueueForOutgoingLinks(Guid recordId, FederationJobType type);

  /// <summary>
  /// Queues several operations at once.
  /// </summary>
  /// <param name="jobs">The operations to queue. Attempts are ignored.</param>
  /// <returns>The pending audit log entries of the queued operations of records.</returns>
  Task<IList<AuditEntry>> EnqueueRange(IEnumerable<FederationJob> jobs);

  /// <summary>
  /// Returns the ids of the given records which have an operation of the given type queued for the partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="type">Type of the operation.</param>
  /// <param name="recordIds">Ids of the local records.</param>
  /// <returns>Ids of the records with a queued operation.</returns>
  ISet<Guid> PendingRecordIds(Guid peerId, FederationJobType type, IEnumerable<Guid> recordIds);

  /// <summary>
  /// Returns operations which are due, oldest first.
  /// </summary>
  /// <param name="now">The current date.</param>
  /// <param name="take">Maximum number of operations.</param>
  /// <returns>List of <see cref="FederationJob"/>.</returns>
  IList<FederationJob> Due(DateTime now, int take);

  /// <summary>
  /// Removes a finished operation.
  /// </summary>
  /// <param name="jobId">Id of the operation.</param>
  Task Complete(Guid jobId);

  /// <summary>
  /// Records a failed attempt and schedules the next one.
  /// </summary>
  /// <param name="jobId">Id of the operation.</param>
  /// <param name="error">The error message.</param>
  /// <param name="nextAttemptAt">Date of the next attempt.</param>
  Task Retry(Guid jobId, string error, DateTime nextAttemptAt);

  /// <summary>
  /// Marks an operation as failed finally. It is not executed anymore, but kept until a new operation for the same record and partner replaces it.
  /// </summary>
  /// <param name="jobId">Id of the operation.</param>
  /// <param name="error">The error message.</param>
  Task Fail(Guid jobId, string error);

  /// <summary>
  /// Returns the operations of a record which failed or are retried after an error.
  /// </summary>
  /// <param name="recordId">Id of the record.</param>
  /// <returns>The problems including the instance names of the partners.</returns>
  IList<RecordSyncProblem> ListProblems(Guid recordId);

  /// <summary>
  /// Removes the waiting, retried and failed transfers of the given records to the partner.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="recordIds">Ids of the local records.</param>
  /// <returns>The records and audit log entries of the removed transfers.</returns>
  Task<IList<(Guid RecordId, Guid? AuditEntryId)>> CancelPush(Guid peerId, IEnumerable<Guid> recordIds);

  /// <summary>
  /// Executes all operations against the partner as soon as possible, including failed ones.
  /// </summary>
  /// <param name="peerId">Id of the partner.</param>
  /// <param name="now">The current date.</param>
  Task Reschedule(Guid peerId, DateTime now);

  /// <summary>
  /// Stores the progress of an operation sending a file.
  /// </summary>
  /// <param name="jobId">Id of the operation.</param>
  /// <param name="transferred">The number of bytes the partner received.</param>
  /// <param name="size">The size of the file in bytes.</param>
  Task SetProgress(Guid jobId, long transferred, long size);

  /// <summary>
  /// Returns the progress of the operations sending a file which belong to the given audit log entries.
  /// </summary>
  /// <param name="entryIds">Ids of the audit log entries.</param>
  /// <returns>The transferred bytes and the size by entry id.</returns>
  IDictionary<Guid, (long Transferred, long Size)> Progress(IEnumerable<Guid> entryIds);

  /// <summary>
  /// Returns the transfers of a record to partners which are waiting or running.
  /// </summary>
  /// <param name="recordId">Id of the record.</param>
  /// <returns>The transfers including the instance names of the partners.</returns>
  IList<RecordSyncTransfer> ListTransfers(Guid recordId);
}
