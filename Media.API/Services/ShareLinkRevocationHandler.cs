using Media.Application.Contracts.Services;
using Messages.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

namespace Media.API.Services;

/// <summary>
/// Revokes the share links of a client, which contain records the client may not share anymore, after the client changed, e.g. its groups.
/// </summary>
public class ShareLinkRevocationHandler(IShareLinkRevoker revoker) : IHandleMessages<ClientStateUpdated>
{
  public async Task Handle(ClientStateUpdated message)
  {
    await revoker.RevokeUnallowed(message.ClientId).ConfigureAwait(false);
  }
}
