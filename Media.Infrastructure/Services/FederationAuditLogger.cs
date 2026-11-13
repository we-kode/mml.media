using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class FederationAuditLogger(IFederationAuditRepository repository, IFederationNotifier notifier) : IFederationAuditLogger
{
  public async Task Log(AuditAction action, AuditStatus status, string source, string target, Guid? transactionId = null, IEnumerable<Guid>? recordIds = null, string? message = null, Guid? entryId = null)
  {
    if (entryId.HasValue)
    {
      var updated = await repository.Update(entryId.Value, status, message).ConfigureAwait(false);
      if (updated.HasValue)
      {
        // repeated failures of a retried operation only change the message, admins are informed about new results.
        if (updated.Value.Previous != status)
        {
          await notifier.AuditAdded(updated.Value.Entry).ConfigureAwait(false);
        }
        return;
      }
    }

    var entry = await repository.Add(new AuditEntry
    {
      Action = action,
      Status = status,
      Source = source,
      Target = target,
      TransactionId = transactionId,
      RecordIds = [.. recordIds ?? []],
      Message = message,
    }).ConfigureAwait(false);
    await notifier.AuditAdded(entry).ConfigureAwait(false);
  }

  public async Task Announce(IEnumerable<AuditEntry> entries)
  {
    foreach (var entry in entries)
    {
      await notifier.AuditAdded(entry).ConfigureAwait(false);
    }
  }

  public async Task Resolve(Guid? entryId, string message)
  {
    if (!entryId.HasValue)
    {
      return;
    }

    var updated = await repository.Update(entryId.Value, AuditStatus.Skipped, message, onlyPending: true).ConfigureAwait(false);
    if (updated.HasValue)
    {
      await notifier.AuditAdded(updated.Value.Entry).ConfigureAwait(false);
    }
  }
}
