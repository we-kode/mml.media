using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

/// <summary>
/// Deletes stored covers, which are not used by any record anymore. Runs on startup and then once a day.
/// </summary>
public class CleanupCovers(IRecordRepository recordRepository, ICoverLoader coverLoader) : BackgroundService
{
  private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

  // covers are saved before the record is stored, so recently written files are kept.
  private static readonly TimeSpan MinAge = TimeSpan.FromHours(1);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(Interval);
    do
    {
      try
      {
        Cleanup();
      }
      catch (Exception e)
      {
        Console.WriteLine($"Cleanup of covers failed: {e.Message}");
      }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
  }

  /// <summary>
  /// Deletes all covers older than one hour, which are not used by any record.
  /// </summary>
  public void Cleanup()
  {
    var used = recordRepository.GetUsedCovers();
    foreach (var fileName in coverLoader.List(DateTime.UtcNow - MinAge).Where(name => !used.Contains(name)).ToList())
    {
      coverLoader.Delete(fileName);
    }
  }
}
