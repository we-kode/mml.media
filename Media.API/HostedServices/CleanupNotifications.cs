using Media.Application.Contracts.Repositories;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

/// <summary>
/// Deletes notifications, whose validity ended. Runs on startup and then once an hour.
/// </summary>
public class CleanupNotifications(INotificationRepository repository) : BackgroundService
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
      }
      catch (Exception e)
      {
        Console.WriteLine($"Cleanup of notifications failed: {e.Message}");
      }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
  }
}
