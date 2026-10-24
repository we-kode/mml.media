using Media.Application.Constants;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class FederationService(
  FederationOptions options,
  IFederationPeerRepository peerRepository,
  IFederationJobRepository jobRepository,
  IFederationInvitationRepository invitationRepository,
  IFederationClient client,
  IFederationAssertionService assertions,
  IFederationKeyStore keyStore,
  IFederationAuditLogger audit,
  IFederationNotifier notifier,
  IRecordRepository recordRepository,
  IRecordService recordService,
  IFederationUploadStore uploadStore,
  IMemoryCache cache) : IFederationService
{
  private const string CLAIM_REQUEST_ID = "request_id";
  private const string CLAIM_BASE_URL = "base_url";
  private const string CLAIM_ACCEPTED = "accepted";
  private const string CLAIM_INVITATION = "invitation";
  private const string REFRESH_CACHE_KEY = "MML_FEDERATION_KEY_REFRESH_";
  private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

  public FederationDiscovery GetDiscovery()
  {
    EnsureEnabled();
    return new FederationDiscovery(Env.INSTANCE, FederationOptions.ProtocolVersion, options.Endpoint, options.JwksUri);
  }

  public async Task<Invitation> CreateInvitation(int validityHours)
  {
    EnsureEnabled();
    var now = DateTime.UtcNow;
    await invitationRepository.DeleteClosed(now).ConfigureAwait(false);

    var code = FederationInvitationCode.Generate();
    var invitation = await invitationRepository.Create(FederationInvitationCode.Hash(code)!, now.AddHours(validityHours)).ConfigureAwait(false);
    invitation.Code = code;
    invitation.BaseUrl = options.PublicUrl!.TrimEnd('/');
    return invitation;
  }

  public async Task<Peer> RequestRegistration(string baseUrl, string invitationCode)
  {
    EnsureEnabled();
    if (FederationInvitationCode.Hash(invitationCode) == null)
    {
      throw new FederationException(FederationErrors.InvalidInvitation);
    }

    var normalizedUrl = FederationHttpHandler.ValidateUrl(baseUrl).GetLeftPart(UriPartial.Authority);
    var discovery = await client.GetDiscovery(normalizedUrl).ConfigureAwait(false);
    EnsureInstanceAvailable(discovery.Instance);
    var jwks = await client.GetJwks(discovery.JwksUri).ConfigureAwait(false);

    var peer = await peerRepository.Create(new Peer
    {
      Instance = discovery.Instance,
      BaseUrl = normalizedUrl,
      Endpoint = discovery.Endpoint,
      JwksUri = discovery.JwksUri,
      Jwks = jwks,
      JwksUpdatedAt = DateTime.UtcNow,
      State = PeerState.Requested,
      RequestId = Guid.NewGuid(),
    }).ConfigureAwait(false);

    try
    {
      using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, "requests", claims: new Dictionary<string, object>
      {
        [CLAIM_REQUEST_ID] = peer.RequestId.ToString(),
        [CLAIM_BASE_URL] = options.PublicUrl!,
        [CLAIM_INVITATION] = invitationCode,
      }).ConfigureAwait(false);

      if (!response.IsSuccessStatusCode)
      {
        throw new FederationException(FederationErrors.RemoteRejected, await ReadErrorCode(response).ConfigureAwait(false));
      }
    }
    catch (FederationException e)
    {
      await peerRepository.Delete(peer.PeerId).ConfigureAwait(false);
      await audit.Log(AuditAction.RegistrationRequested, AuditStatus.Failed, Env.INSTANCE, peer.Instance, message: e.Message).ConfigureAwait(false);
      throw;
    }

    await audit.Log(AuditAction.RegistrationRequested, AuditStatus.Success, Env.INSTANCE, peer.Instance).ConfigureAwait(false);
    await notifier.PeerChanged(peer).ConfigureAwait(false);
    return peer;
  }

  public async Task HandleRegistrationRequest(string assertion)
  {
    EnsureEnabled();
    var issuer = assertions.ReadClaim(assertion, "iss");
    var baseUrl = assertions.ReadClaim(assertion, CLAIM_BASE_URL);
    if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(baseUrl))
    {
      throw new FederationException(FederationErrors.InvalidAssertion);
    }

    // the invitation is checked first, so no partner is contacted without an invitation.
    var invitationHash = FederationInvitationCode.Hash(assertions.ReadClaim(assertion, CLAIM_INVITATION));
    if (invitationHash == null || !invitationRepository.IsOpen(invitationHash, DateTime.UtcNow))
    {
      throw new FederationException(FederationErrors.InvalidInvitation);
    }

    var existing = peerRepository.GetByInstance(issuer);
    if (IsOwnInstance(issuer) || (existing != null && existing.State != PeerState.PendingApproval))
    {
      throw new FederationException(FederationErrors.InstanceConflict);
    }

    var normalizedUrl = FederationHttpHandler.ValidateUrl(baseUrl).GetLeftPart(UriPartial.Authority);
    if (existing != null && !string.Equals(existing.BaseUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase))
    {
      throw new FederationException(FederationErrors.InstanceConflict);
    }

    var discovery = await client.GetDiscovery(normalizedUrl).ConfigureAwait(false);
    if (discovery.Instance != issuer)
    {
      throw new FederationException(FederationErrors.InvalidDiscovery);
    }

    var jwks = await client.GetJwks(discovery.JwksUri).ConfigureAwait(false);
    var result = await assertions.Validate(assertion, issuer, jwks).ConfigureAwait(false);
    if (!result.IsValid || !Guid.TryParse(ClaimValue(result.Claims, CLAIM_REQUEST_ID), out var requestId))
    {
      throw new FederationException(FederationErrors.InvalidAssertion);
    }

    if (!await invitationRepository.Redeem(invitationHash, DateTime.UtcNow, issuer).ConfigureAwait(false))
    {
      throw new FederationException(FederationErrors.InvalidInvitation);
    }

    var peer = existing ?? new Peer { Instance = issuer, State = PeerState.PendingApproval };
    peer.BaseUrl = normalizedUrl;
    peer.Endpoint = discovery.Endpoint;
    peer.JwksUri = discovery.JwksUri;
    peer.Jwks = jwks;
    peer.JwksUpdatedAt = DateTime.UtcNow;
    peer.RequestId = requestId;

    if (existing == null)
    {
      peer = await peerRepository.Create(peer).ConfigureAwait(false);
    }
    else
    {
      await peerRepository.Update(peer).ConfigureAwait(false);
    }

    await audit.Log(AuditAction.RegistrationReceived, AuditStatus.Success, peer.Instance, Env.INSTANCE).ConfigureAwait(false);
    await notifier.RegistrationReceived(peer).ConfigureAwait(false);
  }

  public async Task<Peer> Approve(Guid peerId)
  {
    var peer = GetPeer(peerId);
    if (peer.State != PeerState.PendingApproval)
    {
      throw new FederationException(FederationErrors.InvalidState);
    }

    peer.State = PeerState.ResponsePending;
    await peerRepository.Update(peer).ConfigureAwait(false);

    var informed = await CompleteApproval(peer).ConfigureAwait(false);
    if (!informed)
    {
      await jobRepository.Enqueue(peer.PeerId, FederationJobType.RegistrationResponse).ConfigureAwait(false);
      await notifier.PeerChanged(peer).ConfigureAwait(false);
    }

    await audit.Log(AuditAction.RegistrationApproved, AuditStatus.Success, Env.INSTANCE, peer.Instance,
      message: informed ? null : "Partner could not be informed yet. The answer is retried.").ConfigureAwait(false);
    return peer;
  }

  public async Task<bool> CompleteApproval(Peer peer)
  {
    if (peer.State != PeerState.ResponsePending)
    {
      return true;
    }

    if (!await SendRegistrationResponse(peer, accepted: true).ConfigureAwait(false))
    {
      return false;
    }

    peer.State = PeerState.Linked;
    await peerRepository.Update(peer).ConfigureAwait(false);
    await notifier.PeerChanged(peer).ConfigureAwait(false);
    return true;
  }

  public async Task Reject(Guid peerId)
  {
    var peer = GetPeer(peerId);
    if (peer.State != PeerState.PendingApproval)
    {
      throw new FederationException(FederationErrors.InvalidState);
    }

    var informed = await SendRegistrationResponse(peer, accepted: false).ConfigureAwait(false);
    await peerRepository.Delete(peer.PeerId).ConfigureAwait(false);
    await audit.Log(AuditAction.RegistrationRejected, informed ? AuditStatus.Success : AuditStatus.Failed, Env.INSTANCE, peer.Instance,
      message: informed ? null : "Partner could not be informed.").ConfigureAwait(false);
    peer.State = PeerState.Rejected;
    await notifier.PeerChanged(peer).ConfigureAwait(false);
  }

  public async Task HandleRegistrationResponse(string assertion)
  {
    var authenticated = await Authenticate(assertion, PeerState.Requested).ConfigureAwait(false)
      ?? throw new FederationException(FederationErrors.InvalidAssertion);
    var (peer, claims) = authenticated;

    if (!Guid.TryParse(ClaimValue(claims, CLAIM_REQUEST_ID), out var requestId) || requestId != peer.RequestId
      || !bool.TryParse(ClaimValue(claims, CLAIM_ACCEPTED), out var accepted))
    {
      throw new FederationException(FederationErrors.InvalidAssertion);
    }

    peer.State = accepted ? PeerState.Linked : PeerState.Rejected;
    await peerRepository.Update(peer).ConfigureAwait(false);
    await audit.Log(accepted ? AuditAction.RegistrationApproved : AuditAction.RegistrationRejected, AuditStatus.Success, peer.Instance, Env.INSTANCE).ConfigureAwait(false);
    await notifier.PeerChanged(peer).ConfigureAwait(false);
  }

  public async Task Unlink(Guid peerId, bool deleteRecords, bool force)
  {
    var peer = GetPeer(peerId);
    var informed = true;
    if (peer.State is PeerState.Requested or PeerState.ResponsePending or PeerState.Linked)
    {
      informed = await SendUnlink(peer, deleteRecords).ConfigureAwait(false);
      if (!informed && !force)
      {
        await audit.Log(AuditAction.Unlinked, AuditStatus.Failed, Env.INSTANCE, peer.Instance, message: "Partner could not be informed.").ConfigureAwait(false);
        throw new FederationException(FederationErrors.RemoteUnreachable);
      }
    }

    if (deleteRecords)
    {
      await RemoveRecordsOf(peer, Env.INSTANCE).ConfigureAwait(false);
    }

    await DeletePeer(peer).ConfigureAwait(false);
    await audit.Log(AuditAction.Unlinked, informed ? AuditStatus.Success : AuditStatus.Failed, Env.INSTANCE, peer.Instance,
      message: informed ? null : "Partner could not be informed. The registration and records remain on the partner.").ConfigureAwait(false);
    await notifier.PeerChanged(peer).ConfigureAwait(false);
  }

  public async Task HandleUnlink(Peer peer, bool deleteOwnerRecords)
  {
    if (deleteOwnerRecords)
    {
      await RemoveRecordsOf(peer, peer.Instance).ConfigureAwait(false);
    }

    await DeletePeer(peer).ConfigureAwait(false);
    await audit.Log(AuditAction.Unlinked, AuditStatus.Success, peer.Instance, Env.INSTANCE,
      message: deleteOwnerRecords ? "Records of the partner were removed." : "Records of the partner were kept.").ConfigureAwait(false);
    await notifier.PeerChanged(peer).ConfigureAwait(false);
  }

  public async Task<(Peer Peer, IDictionary<string, object> Claims)?> Authenticate(string assertion, params PeerState[] states)
  {
    var issuer = assertions.ReadClaim(assertion, "iss");
    if (string.IsNullOrWhiteSpace(issuer))
    {
      return null;
    }

    var peer = peerRepository.GetByInstance(issuer);
    if (peer == null || peer.Instance != issuer || !states.Contains(peer.State))
    {
      return null;
    }

    var result = await assertions.Validate(assertion, peer.Instance, peer.Jwks).ConfigureAwait(false);
    if (result.UnknownKey && MayRefresh(peer))
    {
      try
      {
        peer = await RefreshKeys(peer).ConfigureAwait(false);
        result = await assertions.Validate(assertion, peer.Instance, peer.Jwks).ConfigureAwait(false);
      }
      catch (FederationException)
      {
        return null;
      }
    }

    return result.IsValid ? (peer, result.Claims) : null;
  }

  public async Task<Peer> RefreshKeys(Peer peer)
  {
    var jwks = await client.GetJwks(peer.JwksUri).ConfigureAwait(false);
    var changed = jwks != peer.Jwks;
    peer.Jwks = jwks;
    peer.JwksUpdatedAt = DateTime.UtcNow;
    await peerRepository.Update(peer).ConfigureAwait(false);

    if (changed)
    {
      await audit.Log(AuditAction.KeysChanged, AuditStatus.Success, peer.Instance, Env.INSTANCE, message: "Public keys of the partner were updated.").ConfigureAwait(false);
    }

    return peer;
  }

  public async Task RotateKeys()
  {
    if (!options.IsEnabled || !keyStore.Rotate(DateTime.UtcNow))
    {
      return;
    }

    foreach (var peer in peerRepository.ListByState(PeerState.Linked, PeerState.ResponsePending))
    {
      await jobRepository.Enqueue(peer.PeerId, FederationJobType.KeysChanged).ConfigureAwait(false);
    }
  }

  private async Task<bool> SendRegistrationResponse(Peer peer, bool accepted)
  {
    try
    {
      using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, "responses", claims: new Dictionary<string, object>
      {
        [CLAIM_REQUEST_ID] = peer.RequestId.ToString(),
        [CLAIM_ACCEPTED] = accepted,
      }).ConfigureAwait(false);
      return response.IsSuccessStatusCode;
    }
    catch (FederationException)
    {
      return false;
    }
  }

  private async Task<bool> SendUnlink(Peer peer, bool deleteOwnerRecords)
  {
    try
    {
      using var response = await client.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, "unlink", JsonContent.Create(new { deleteOwnerRecords })).ConfigureAwait(false);
      // the partner does not know this instance anymore, so it is unlinked already.
      return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized;
    }
    catch (FederationException)
    {
      return false;
    }
  }

  private async Task RemoveRecordsOf(Peer peer, string initiator)
  {
    var recordIds = recordRepository.GetRecordIdsByOwner(peer.Instance);
    foreach (var recordId in recordIds)
    {
      await recordService.DeleteRecord(recordId, removeLocked: true).ConfigureAwait(false);
    }

    if (recordIds.Count > 0)
    {
      await audit.Log(AuditAction.RecordsRemoved, AuditStatus.Success, initiator, Env.INSTANCE, recordIds: recordIds,
        message: $"{recordIds.Count} records of '{peer.Instance}' were removed.").ConfigureAwait(false);
    }
  }

  private async Task DeletePeer(Peer peer)
  {
    uploadStore.RemoveAll(peer.PeerId);
    await peerRepository.Delete(peer.PeerId).ConfigureAwait(false);
  }

  private bool MayRefresh(Peer peer)
  {
    var cacheKey = $"{REFRESH_CACHE_KEY}{peer.PeerId}";
    if (cache.TryGetValue(cacheKey, out _))
    {
      return false;
    }

    cache.Set(cacheKey, true, RefreshInterval);
    return true;
  }

  private void EnsureEnabled()
  {
    if (!options.IsEnabled)
    {
      throw new FederationException(FederationErrors.Disabled);
    }
  }

  private void EnsureInstanceAvailable(string instance)
  {
    if (IsOwnInstance(instance) || peerRepository.GetByInstance(instance) != null)
    {
      throw new FederationException(FederationErrors.InstanceConflict);
    }
  }

  private static bool IsOwnInstance(string instance) => string.Equals(instance, Env.INSTANCE, StringComparison.OrdinalIgnoreCase);

  private static string? ClaimValue(IDictionary<string, object> claims, string name) => claims.TryGetValue(name, out var value) ? value?.ToString() : null;

  private Peer GetPeer(Guid peerId) => peerRepository.Get(peerId) ?? throw new FederationException(FederationErrors.NotFound);

  private static async Task<string> ReadErrorCode(HttpResponseMessage response)
  {
    try
    {
      var error = await response.Content.ReadFromJsonAsync<FederationError>().ConfigureAwait(false);
      return error?.Code ?? ((int)response.StatusCode).ToString();
    }
    catch (Exception)
    {
      return ((int)response.StatusCode).ToString();
    }
  }

  private sealed record FederationError(string Code);
}
