using Media.API.Extensions;
using Media.Application.Contracts.Repositories;
using Media.Application.Models.Options;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

/// <summary>
/// Deletes share links, which are expired or used, and tokens of shared searches, which were not opened for the configured number of days. Runs on startup and
/// then once an hour.
/// </summary>
public class CleanupShareLinks(IShareLinkRepository repository, IShareQueryRepository queryRepository, ISettingRepository settingRepository) : BackgroundService
{
  private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(Interval);
    do
    {
      try
      {
        await repository.RemoveExpired(DateTime.UtcNow).ConfigureAwait(false);
        await queryRepository.RemoveUnused(DateTime.UtcNow.AddDays(-settingRepository.LoadShareSettings().SearchLinkRetentionDays)).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        Console.WriteLine($"Cleanup of share links failed: {e.Message}");
      }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
  }
}
