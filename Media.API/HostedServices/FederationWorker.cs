using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

internal class FederationWorker(
  FederationOptions options,
  IFederationService federationService,
  IFederationJobProcessor jobProcessor,
  IFederationPeerRepository peerRepository,
  IFederationSyncService syncService,
  IFederationAuditRepository auditRepository,
  ISettingRepository settingRepository,
  ILogger<FederationWorker> logger) : BackgroundService
{
  private static readonly TimeSpan JobInterval = TimeSpan.FromSeconds(15);
  private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromHours(6);
  private static readonly TimeSpan KeyRefreshAge = TimeSpan.FromDays(1);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (!options.IsEnabled)
    {
      return;
    }

    var nextMaintenance = DateTime.MinValue;
    using var timer = new PeriodicTimer(JobInterval);
    do
    {
      var now = DateTime.UtcNow;
      try
      {
        if (now >= nextMaintenance)
        {
          await Maintain(now).ConfigureAwait(false);
          nextMaintenance = now.Add(MaintenanceInterval);
        }

        await jobProcessor.ProcessDue(now).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Federation worker failed.");
      }
    } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
  }

  private async Task Maintain(DateTime now)
  {
    await federationService.RotateKeys().ConfigureAwait(false);
    await federationService.AnnounceAddress().ConfigureAwait(false);
    await syncService.DiscardStale(now.AddDays(-options.UploadRetentionDays)).ConfigureAwait(false);

    var retentionDays = AuditRetention.Days(settingRepository.Get(AuditRetention.SettingKey, string.Empty));
    if (retentionDays > 0)
    {
      await auditRepository.DeleteOlderThan(now.AddDays(-retentionDays)).ConfigureAwait(false);
    }

    foreach (var peer in peerRepository.ListByState(PeerState.Requested, PeerState.ResponsePending, PeerState.Linked))
    {
      if (peer.JwksUpdatedAt.HasValue && now - peer.JwksUpdatedAt.Value < KeyRefreshAge)
      {
        continue;
      }

      try
      {
        await federationService.RefreshKeys(peer).ConfigureAwait(false);
      }
      catch (FederationException e)
      {
        logger.LogWarning("Public keys of partner '{Instance}' could not be refreshed: {Code}", peer.Instance, e.Code);
      }
    }
  }
}
