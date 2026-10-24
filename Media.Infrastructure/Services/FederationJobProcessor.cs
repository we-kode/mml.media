using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class FederationJobProcessor(
  IFederationJobRepository jobRepository,
  IFederationPeerRepository peerRepository,
  IFederationClient client,
  IFederationService federationService,
  IFederationSyncService syncService,
  IFederationAuditLogger audit) : IFederationJobProcessor
{
  private const int BATCH_SIZE = 20;
  private const int MAX_ATTEMPTS = 30;
  private static readonly TimeSpan MaxDelay = TimeSpan.FromHours(6);

  public async Task ProcessDue(DateTime now)
  {
    foreach (var job in jobRepository.Due(now, BATCH_SIZE))
    {
      var peer = peerRepository.Get(job.PeerId);
      if (peer == null)
      {
        await jobRepository.Complete(job.JobId).ConfigureAwait(false);
        await audit.Resolve(job.AuditEntryId, "The partner was removed.").ConfigureAwait(false);
        continue;
      }

      FederationJobResult result;
      try
      {
        result = await Execute(job, peer).ConfigureAwait(false);
      }
      catch (FederationException e)
      {
        result = FederationJobResult.Retry(e.Code);
      }

      if (result.Done)
      {
        if (result.Error == null)
        {
          await jobRepository.Complete(job.JobId).ConfigureAwait(false);
          await audit.Resolve(job.AuditEntryId, "The operation was not necessary anymore.").ConfigureAwait(false);
        }
        else
        {
          await Fail(job, peer, result.Error).ConfigureAwait(false);
        }
        continue;
      }

      if (job.Attempts + 1 >= MAX_ATTEMPTS)
      {
        await Fail(job, peer, $"Gave up after {MAX_ATTEMPTS} attempts: {result.Error}").ConfigureAwait(false);
        continue;
      }

      var delay = TimeSpan.FromMinutes(Math.Min(Math.Pow(2, job.Attempts), MaxDelay.TotalMinutes));
      var nextAttemptAt = now.Add(delay);

      // the entry of the operation shows every failed attempt, operations without entry log only the first one.
      if (job.AuditEntryId.HasValue || job.Attempts == 0)
      {
        await LogFailure(job, peer, $"{result.Error} The operation is retried at {nextAttemptAt:u}.").ConfigureAwait(false);
      }

      await jobRepository.Retry(job.JobId, result.Error ?? string.Empty, nextAttemptAt).ConfigureAwait(false);
    }
  }

  private async Task<FederationJobResult> Execute(FederationJob job, Peer peer)
  {
    return job.Type switch
    {
      FederationJobType.RegistrationResponse => await federationService.CompleteApproval(peer).ConfigureAwait(false)
        ? FederationJobResult.Success
        : FederationJobResult.Retry("Partner could not be informed about the approval."),
      FederationJobType.KeysChanged => await SendKeysChanged(peer).ConfigureAwait(false),
      _ => await syncService.Execute(job, peer).ConfigureAwait(false),
    };
  }

  private async Task<FederationJobResult> SendKeysChanged(Peer peer)
  {
    using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, "keys-changed").ConfigureAwait(false);
    var result = FederationJobResult.FromResponse(response);
    if (result.Done && result.Error == null)
    {
      await audit.Log(AuditAction.KeysChanged, AuditStatus.Success, Env.INSTANCE, peer.Instance, message: "Partner was informed about new public keys.").ConfigureAwait(false);
    }

    return result;
  }

  // failed operations of existing records are kept, so the records are marked with a problem.
  private async Task Fail(FederationJob job, Peer peer, string error)
  {
    if (job.RecordId.HasValue && job.Type != FederationJobType.DeleteRecord)
    {
      await jobRepository.Fail(job.JobId, error).ConfigureAwait(false);
    }
    else
    {
      await jobRepository.Complete(job.JobId).ConfigureAwait(false);
    }

    await LogFailure(job, peer, error).ConfigureAwait(false);
  }

  private async Task LogFailure(FederationJob job, Peer peer, string error)
  {
    await audit.Log(job.Type.ToAuditAction(), AuditStatus.Failed, Env.INSTANCE, peer.Instance, job.TransactionId,
      job.RecordId.HasValue ? [job.RecordId.Value] : null, error, job.AuditEntryId).ConfigureAwait(false);
  }
}
