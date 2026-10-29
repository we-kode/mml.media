using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.API.Filters;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

/// <summary>
/// Administration of partner instances and the federation audit log.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
[TypeFilter(typeof(FederationExceptionFilter))]
public class FederationController(
  IFederationService federationService,
  IFederationPeerRepository peerRepository,
  IFederationAuditRepository auditRepository,
  IFederationInvitationRepository invitationRepository,
  IFederationRuleRepository ruleRepository,
  IFederationSyncService syncService,
  IRecordRepository recordRepository) : ControllerBase
{
  /// <summary>
  /// Loads a list of partner instances.
  /// </summary>
  /// <param name="filter">Filter by instance name or url.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="Peers"/></returns>
  [HttpGet("peers")]
  public ActionResult<Peers> ListPeers([FromQuery] string? filter, [FromQuery] int skip = List.Skip, [FromQuery] int take = List.Take)
  {
    return peerRepository.List(filter, skip, take);
  }

  /// <summary>
  /// Requests the registration at a partner instance.
  /// </summary>
  /// <response code="400">If the url is invalid or the partner returns invalid information.</response>
  /// <response code="409">If the instance name of the partner is already used.</response>
  /// <response code="403">If the invitation code has an invalid format.</response>
  /// <response code="502">If the partner is not reachable or rejects the request, e.g. because the invitation is invalid.</response>
  [HttpPost("peers")]
  public async Task<ActionResult<Peer>> RequestRegistration([FromBody] PeerRegistrationRequest request)
  {
    return await federationService.RequestRegistration(request.BaseUrl, request.InvitationCode).ConfigureAwait(false);
  }

  /// <summary>
  /// Loads the open invitations. Codes are not returned.
  /// </summary>
  /// <returns><see cref="Invitations"/></returns>
  [HttpGet("invitations")]
  public ActionResult<Invitations> ListInvitations()
  {
    return invitationRepository.ListOpen(DateTime.UtcNow);
  }

  /// <summary>
  /// Creates a one-time invitation. The returned code must be passed to the admin of the partner, who requests the registration with it.
  /// </summary>
  /// <param name="request">The validity of the invitation. Without request the invitation is valid for 24 hours.</param>
  /// <response code="400">If the validity is not between 1 hour and 90 days.</response>
  /// <response code="404">If the federation is disabled.</response>
  [HttpPost("invitations")]
  public async Task<ActionResult<Invitation>> CreateInvitation([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvitationRequest? request)
  {
    return await federationService.CreateInvitation(request?.ValidityHours ?? Invitation.DefaultValidityHours).ConfigureAwait(false);
  }

  /// <summary>
  /// Revokes an open invitation.
  /// </summary>
  /// <param name="id">Id of the invitation.</param>
  [HttpDelete("invitations/{id:guid}")]
  public async Task<IActionResult> DeleteInvitation(Guid id)
  {
    await invitationRepository.Delete(id).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Approves the registration request of a partner. The synchronization starts once the partner is informed.
  /// </summary>
  /// <response code="404">If the partner does not exist.</response>
  /// <response code="409">If the partner does not wait for approval.</response>
  [HttpPost("peers/{id:guid}/approve")]
  public async Task<ActionResult<Peer>> Approve(Guid id)
  {
    return await federationService.Approve(id).ConfigureAwait(false);
  }

  /// <summary>
  /// Rejects and removes the registration request of a partner.
  /// </summary>
  /// <response code="404">If the partner does not exist.</response>
  /// <response code="409">If the partner does not wait for approval.</response>
  [HttpPost("peers/{id:guid}/reject")]
  public async Task<IActionResult> Reject(Guid id)
  {
    await federationService.Reject(id).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Removes the registration of a partner. Records are only removed if requested.
  /// </summary>
  /// <response code="404">If the partner does not exist.</response>
  /// <response code="502">If the partner cannot be informed and force is not set.</response>
  [HttpPost("peers/{id:guid}/unlink")]
  [ProducesResponseType(StatusCodes.Status502BadGateway)]
  public async Task<IActionResult> Unlink(Guid id, [FromBody] PeerUnlinkRequest request)
  {
    await federationService.Unlink(id, request.DeleteRecords, request.Force).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Changes the address of a partner, e.g. after it moved. The partner at the new address must have the same instance
  /// name and publish at least one of its known public keys.
  /// </summary>
  /// <response code="400">If the new address is invalid or the partner there does not publish a known key.</response>
  /// <response code="404">If the partner does not exist.</response>
  /// <response code="502">If the partner is not reachable at the new address.</response>
  [HttpPost("peers/{id:guid}/address")]
  [ProducesResponseType(StatusCodes.Status502BadGateway)]
  public async Task<ActionResult<Peer>> ChangeAddress(Guid id, [FromBody] PeerAddressRequest request)
  {
    var peer = peerRepository.Get(id) ?? throw new FederationException(FederationErrors.NotFound);
    return await federationService.ChangeAddress(peer, request.BaseUrl).ConfigureAwait(false);
  }

  /// <summary>
  /// Loads the rules, oldest first.
  /// </summary>
  /// <param name="direction">Only rules of this direction are returned if set.</param>
  /// <param name="peerId">Only rules which apply to this partner are returned if set, i.e. the rules for all partners and the rules restricted to the partner.</param>
  /// <returns><see cref="Rules"/></returns>
  [HttpGet("rules")]
  public ActionResult<Rules> ListRules([FromQuery] SyncDirection? direction, [FromQuery] Guid? peerId)
  {
    return peerId.HasValue ? ruleRepository.ListFor(peerId.Value, direction) : ruleRepository.List(direction);
  }

  /// <summary>
  /// Creates a rule. A rule without partners applies to all partners. Rules only apply to records added afterwards or synchronized manually.
  /// </summary>
  /// <param name="request">The rule.</param>
  /// <response code="400">If a partner, artist, album or genre does not exist.</response>
  [HttpPost("rules")]
  public async Task<ActionResult<Rule>> CreateRule([FromBody] RuleRequest request)
  {
    return await ruleRepository.Create(ToRule(request)).ConfigureAwait(false);
  }

  /// <summary>
  /// Changes a rule.
  /// </summary>
  /// <param name="ruleId">Id of the rule.</param>
  /// <param name="request">The changed rule.</param>
  /// <response code="400">If a partner, artist, album or genre does not exist.</response>
  /// <response code="404">If the rule does not exist.</response>
  [HttpPost("rules/{ruleId:guid}")]
  public async Task<IActionResult> UpdateRule(Guid ruleId, [FromBody] RuleRequest request)
  {
    if (ruleRepository.Get(ruleId) == null)
    {
      return NotFound();
    }

    var rule = ToRule(request);
    rule.RuleId = ruleId;
    await ruleRepository.Update(rule).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Removes the given rules.
  /// </summary>
  /// <param name="ids">Ids of the rules.</param>
  [HttpPost("rules/deleteList")]
  public async Task<IActionResult> DeleteRules([FromBody] IList<Guid> ids)
  {
    await ruleRepository.Delete(ids).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Loads the names of all instances owning records.
  /// </summary>
  /// <returns>The instance names.</returns>
  [HttpGet("instances")]
  public ActionResult<IList<string>> ListInstances()
  {
    return Ok(recordRepository.GetOwnerInstances());
  }

  /// <summary>
  /// Loads a list of audit log entries matching the filter in its sort order, newest first by default.
  /// </summary>
  /// <param name="query">Filter and sort order of the entries.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="AuditEntries"/></returns>
  [HttpGet("audit")]
  public ActionResult<AuditEntries> ListAudit([FromQuery] AuditFilter query, [FromQuery] int skip = List.Skip, [FromQuery] int take = List.Take)
  {
    var entries = auditRepository.List(query.Map(), skip, take);
    syncService.AddProgress(entries.Items);
    return entries;
  }

  /// <summary>
  /// Loads the names of all partner instances occurring in the audit log.
  /// </summary>
  /// <returns>The instance names.</returns>
  [HttpGet("audit/instances")]
  public ActionResult<IList<string>> ListAuditInstances()
  {
    return Ok(auditRepository.ListInstances());
  }

  /// <summary>
  /// Removes the given audit log entries.
  /// </summary>
  /// <param name="ids">Ids of the entries.</param>
  [HttpPost("audit/deleteList")]
  public async Task<IActionResult> DeleteAudit([FromBody] IList<Guid> ids)
  {
    await auditRepository.Delete(ids).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Removes all audit log entries matching the filter. All entries are removed without filter.
  /// </summary>
  /// <param name="query">Filter of the entries. The sort order is ignored.</param>
  /// <returns>The count of removed entries.</returns>
  [HttpDelete("audit")]
  public async Task<ActionResult<int>> ClearAudit([FromQuery] AuditFilter query)
  {
    return Ok(await auditRepository.Delete(query.Map()).ConfigureAwait(false));
  }

  private static Rule ToRule(RuleRequest request)
  {
    var rule = new Rule
    {
      Direction = request.Direction,
      Peers = [.. request.Peers.Distinct().Select(id => new Peer { PeerId = id })],
      Artists = [.. request.Artists.Distinct().Select(id => new Artist { ArtistId = id })],
      Albums = [.. request.Albums.Distinct().Select(id => new Album { AlbumId = id })],
      Genres = [.. request.Genres.Distinct().Select(id => new Genre { GenreId = id })],
      Groups = [.. request.Groups.Distinct().Select(id => new Group(id, string.Empty, false))],
    };

    return rule;
  }
}
