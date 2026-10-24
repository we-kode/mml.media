using Media.API.Sockets;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Media.API.Services;

public class FederationNotifier(IHubContext<FederationHub> hubContext) : IFederationNotifier
{
  public Task RegistrationReceived(Peer peer) => hubContext.Clients.All.SendAsync(FederationHub.RequestReceived, peer);

  public Task PeerChanged(Peer peer) => hubContext.Clients.All.SendAsync(FederationHub.StateChanged, peer);

  public Task AuditAdded(AuditEntry entry) => hubContext.Clients.All.SendAsync(FederationHub.AuditAdded, entry);
}
