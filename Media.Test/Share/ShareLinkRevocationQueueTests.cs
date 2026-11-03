using Media.API.HostedServices;
using Media.Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareLinkRevocationQueueTests
{
  [Fact]
  public async Task Enqueue_RevokesInTheBackgroundAndKeepsRunningAfterErrors()
  {
    var calls = 0;
    var done = new TaskCompletionSource();
    var revoker = new Mock<IShareLinkRevoker>();
    revoker.Setup(r => r.RevokeUnallowed(null)).Returns(() =>
    {
      if (Interlocked.Increment(ref calls) == 1)
      {
        throw new InvalidOperationException("identity unreachable");
      }

      done.TrySetResult();
      return Task.CompletedTask;
    });
    var provider = new ServiceCollection().AddSingleton(revoker.Object).BuildServiceProvider();
    var queue = new ShareLinkRevocationQueue(provider.GetRequiredService<IServiceScopeFactory>());
    await queue.StartAsync(TestContext.Current.CancellationToken);

    queue.Enqueue();
    await WaitUntil(() => Volatile.Read(ref calls) == 1);
    queue.Enqueue();
    await done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    await queue.StopAsync(TestContext.Current.CancellationToken);
    Assert.Equal(2, calls);
  }

  private static async Task WaitUntil(Func<bool> condition)
  {
    for (var i = 0; i < 100 && !condition(); i++)
    {
      await Task.Delay(20, TestContext.Current.CancellationToken);
    }
  }
}
