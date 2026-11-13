using Media.API.Services;
using Media.Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

/// <summary>
/// Checks the share links of clients in the background, whenever it is requested.
/// </summary>
public class ShareLinkRevocationQueue(IServiceScopeFactory scopeFactory) : BackgroundService, IShareLinkRevocationQueue
{
  private readonly Channel<bool> requests = Channel.CreateBounded<bool>(
    new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

  public void Enqueue()
  {
    requests.Writer.TryWrite(true);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await foreach (var _ in requests.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
    {
      try
      {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IShareLinkRevoker>().RevokeUnallowed().ConfigureAwait(false);
      }
      catch (Exception e)
      {
        Console.WriteLine($"Revocation of share links failed: {e.Message}");
      }
    }
  }
}
