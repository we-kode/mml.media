using System;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Executes the queued operations against partner instances.
/// </summary>
public interface IFederationJobProcessor
{
  /// <summary>
  /// Executes all due operations. Failed operations are retried with an increasing delay.
  /// </summary>
  /// <param name="now">The current date.</param>
  Task ProcessDue(DateTime now);
}
