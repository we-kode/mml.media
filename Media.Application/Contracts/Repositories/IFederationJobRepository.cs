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
}
