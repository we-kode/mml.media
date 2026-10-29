using Asp.Versioning;
using Media.API.Authentication;
using Media.API.Contracts;
using Media.API.Filters;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Media.API.Controllers;

/// <summary>
/// Endpoints called by partner instances.
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/federation/peer")]
[TypeFilter(typeof(FederationExceptionFilter))]
public class FederationPeerController(
  IFederationService federationService,
  IFederationKeyStore keyStore,
  IFederationPeerRepository peerRepository,
  IFederationSyncService syncService) : ControllerBase
{
  /// <summary>
  /// Returns the public information of this instance.
  /// </summary>
  /// <response code="404">If the federation is disabled.</response>
  [HttpGet("discovery")]
  [AllowAnonymous]
  public ActionResult<FederationDiscovery> Discovery()
  {
    return federationService.GetDiscovery();
  }

  /// <summary>
  /// Returns the public keys of this instance as JSON web key set.
  /// </summary>
  /// <response code="404">If the federation is disabled.</response>
  [HttpGet("jwks")]
  [AllowAnonymous]
  public IActionResult Jwks()
  {
    federationService.GetDiscovery();
    return Content(keyStore.GetPublicJwks(), "application/jwk-set+json");
  }

  /// <summary>
  /// Receives a signed registration request of a partner.
  /// </summary>
  /// <response code="401">If the request is not signed correctly.</response>
  /// <response code="409">If the instance name of the partner is already used.</response>
  [HttpPost("requests")]
  [AllowAnonymous]
  [ProducesResponseType(StatusCodes.Status202Accepted)]
  public async Task<IActionResult> ReceiveRequest()
  {
    await federationService.HandleRegistrationRequest(BearerAssertion()).ConfigureAwait(false);
    return Accepted();
  }

  /// <summary>
  /// Receives the signed answer of a partner to a registration request.
  /// </summary>
  /// <response code="401">If the answer is not signed correctly or does not match a pending request.</response>
  [HttpPost("responses")]
  [AllowAnonymous]
  public async Task<IActionResult> ReceiveResponse()
  {
    await federationService.HandleRegistrationResponse(BearerAssertion()).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Informs this instance that the partner published new public keys.
  /// </summary>
  [HttpPost("keys-changed")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public async Task<IActionResult> KeysChanged()
  {
    await federationService.RefreshKeys(CurrentPeer()).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Informs this instance that the partner moved to a new address.
  /// </summary>
  /// <response code="400">If the new address is invalid or the partner there does not publish a known key.</response>
  /// <response code="502">If the partner is not reachable at the new address.</response>
  [HttpPost("address-changed")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public async Task<IActionResult> AddressChanged([FromBody] PeerAddressRequest request)
  {
    await federationService.ChangeAddress(CurrentPeer(), request.BaseUrl).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Signs the random value of the calling partner, which checks a new address of this instance.
  /// </summary>
  /// <response code="401">If the value is empty or too long.</response>
  [HttpPost("proof")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public ActionResult<ProofResponse> Proof([FromBody] ProofRequest request)
  {
    return new ProofResponse(federationService.CreateProof(CurrentPeer(), request.Nonce));
  }

  /// <summary>
  /// Removes the registration of the calling partner.
  /// </summary>
  [HttpPost("unlink")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public async Task<IActionResult> Unlink([FromBody] RemoteUnlinkRequest request)
  {
    await federationService.HandleUnlink(CurrentPeer(), request.DeleteOwnerRecords).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Starts or continues the transfer of a record of the calling partner. The multipart form contains
  /// transactionId, metadata, size and sha256 of the file and the optional cover. The file is sent in chunks afterwards.
  /// </summary>
  /// <response code="400">If the record is invalid or too large.</response>
  /// <response code="409">If the partner is not linked.</response>
  /// <response code="429">If the partner has too many open transfers.</response>
  [HttpPost("records")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  [DisableRequestSizeLimit]
  [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
  public async Task<ActionResult<ReceiveResult>> StartRecord()
  {
    var peer = CurrentLinkedPeer();
    var form = await Request.ReadFormAsync().ConfigureAwait(false);
    if (!Guid.TryParse(form["transactionId"], out var transactionId) || !long.TryParse(form["size"], out var size))
    {
      throw new FederationException(FederationErrors.InvalidRecord);
    }

    return await syncService.StartReceive(peer, transactionId, ReadMetadata(form), size, form["sha256"].ToString(), await ReadCover(form).ConfigureAwait(false)).ConfigureAwait(false);
  }

  /// <summary>
  /// Appends a chunk of the file to a pending transfer. The body contains the raw bytes starting at the given offset.
  /// </summary>
  /// <param name="transactionId">The transaction id of the transfer.</param>
  /// <param name="offset">The position of the chunk in the file.</param>
  /// <response code="404">If the transfer does not exist.</response>
  /// <response code="409">If the offset does not match the received bytes. The answer contains the expected offset.</response>
  [HttpPost("records/{transactionId:guid}/content")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  [DisableRequestSizeLimit]
  public async Task<ActionResult<ReceiveResult>> AppendRecordContent(Guid transactionId, [FromQuery] long offset)
  {
    var (accepted, received) = await syncService.AppendChunk(CurrentLinkedPeer(), transactionId, offset, Request.Body).ConfigureAwait(false);
    var result = ReceiveResult.Pending(received);
    return accepted ? Ok(result) : Conflict(result);
  }

  /// <summary>
  /// Finishes a transfer and stores the record.
  /// </summary>
  /// <param name="transactionId">The transaction id of the transfer.</param>
  /// <response code="400">If the file is not an mp3 file.</response>
  /// <response code="404">If the transfer does not exist.</response>
  /// <response code="409">If the file is not complete or corrupted. The answer contains the offset to continue at.</response>
  [HttpPost("records/{transactionId:guid}/complete")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public async Task<ActionResult<ReceiveResult>> CompleteRecord(Guid transactionId)
  {
    var result = await syncService.CompleteReceive(CurrentLinkedPeer(), transactionId).ConfigureAwait(false);
    return result.Status == ReceiveStatus.Pending ? Conflict(result) : Ok(result);
  }

  /// <summary>
  /// Updates the metadata of a record of the calling partner. The multipart form contains the metadata and the optional cover.
  /// </summary>
  /// <param name="transactionId">The transaction id the record was sent with.</param>
  /// <response code="404">If the record does not exist on this instance.</response>
  [HttpPost("records/{transactionId:guid}")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  [DisableRequestSizeLimit]
  [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
  public async Task<IActionResult> UpdateRecord(Guid transactionId)
  {
    var peer = CurrentLinkedPeer();
    var form = await Request.ReadFormAsync().ConfigureAwait(false);
    var updated = await syncService.ReceiveUpdate(peer, transactionId, ReadMetadata(form), await ReadCover(form).ConfigureAwait(false)).ConfigureAwait(false);
    return updated ? Ok() : NotFound();
  }

  /// <summary>
  /// Removes a record of the calling partner.
  /// </summary>
  /// <param name="transactionId">The transaction id the record was sent with.</param>
  /// <response code="404">If the record does not exist on this instance.</response>
  [HttpDelete("records/{transactionId:guid}")]
  [Authorize(AuthenticationSchemes = FederationPeerAuthenticationHandler.SchemeName)]
  public async Task<IActionResult> DeleteRecord(Guid transactionId)
  {
    var deleted = await syncService.ReceiveDeletion(CurrentLinkedPeer(), transactionId).ConfigureAwait(false);
    return deleted ? Ok() : NotFound();
  }

  private static SyncedRecord ReadMetadata(IFormCollection form)
  {
    try
    {
      return JsonSerializer.Deserialize<SyncedRecord>(form["metadata"].ToString(), JsonSerializerOptions.Web)
        ?? throw new FederationException(FederationErrors.InvalidRecord);
    }
    catch (JsonException)
    {
      throw new FederationException(FederationErrors.InvalidRecord);
    }
  }

  private static async Task<byte[]?> ReadCover(IFormCollection form)
  {
    var cover = form.Files["cover"];
    if (cover == null || cover.Length == 0)
    {
      return null;
    }

    using var memory = new MemoryStream();
    await cover.CopyToAsync(memory).ConfigureAwait(false);
    return memory.ToArray();
  }

  private Peer CurrentLinkedPeer()
  {
    var peer = CurrentPeer();
    if (peer.State is not (PeerState.Linked or PeerState.ResponsePending))
    {
      throw new FederationException(FederationErrors.InvalidState);
    }

    return peer;
  }

  private string BearerAssertion()
  {
    var header = HttpContext.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
      throw new FederationException(FederationErrors.InvalidAssertion);
    }

    return header["Bearer ".Length..].Trim();
  }

  private Peer CurrentPeer()
  {
    var peerId = User.FindFirst(FederationPeerAuthenticationHandler.PeerIdClaim)?.Value;
    return (Guid.TryParse(peerId, out var id) ? peerRepository.Get(id) : null)
      ?? throw new FederationException(FederationErrors.InvalidAssertion);
  }
}
