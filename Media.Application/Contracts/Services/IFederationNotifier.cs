using Media.Application.Models.Federation;
using System.Threading.Tasks;

namespace Media.Application.Contracts.Services;

/// <summary>
/// Informs connected admins about federation events.
/// </summary>
public interface IFederationNotifier
{
  /// <summary>
  /// Informs about a registration request of a partner waiting for approval.
  /// </summary>
  /// <param name="peer">The requesting partner.</param>
  Task RegistrationReceived(Peer peer);

  /// <summary>
  /// Informs about a changed or removed partner.
  /// </summary>
  /// <param name="peer">The changed partner.</param>
  Task PeerChanged(Peer peer);

  /// <summary>
  /// Informs about a new audit log entry.
  /// </summary>
  /// <param name="entry">The new entry.</param>
  Task AuditAdded(AuditEntry entry);

  /// <summary>
  /// Informs about the progress of a transfer of a record.
  /// </summary>
  /// <param name="progress">The current progress.</param>
  Task Progress(TransferProgress progress);
}
