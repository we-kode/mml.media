using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.API.Filters;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

/// <summary>
/// Synchronization of records with partner instances.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[TypeFilter(typeof(FederationExceptionFilter))]
public class SyncController(
  IFederationSyncService syncService,
  IFederationLinkRepository linkRepository,
  IFederationJobRepository jobRepository,
  IRecordRepository recordRepository) : ControllerBase
{
  /// <summary>
  /// The instance name of the running service.
  /// </summary>
  /// <returns>Instance name.</returns>
  [HttpGet("instance")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<string> GetInstance()
  {
    return Env.INSTANCE;
  }

  /// <summary>
  /// Sends the given own records to the selected or all linked partners with a matching outgoing rule, if they are not sent already.
  /// The given records are sent even if their synchronization was revoked, records of the folders are not.
  /// </summary>
  /// <param name="request">Records, folders and partners to synchronize.</param>
  /// <returns><see cref="SyncResult"/></returns>
  [HttpPost]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<ActionResult<SyncResult>> Synchronize([FromBody] SyncRequest request)
  {
    var peerIds = request.PeerIds?.ToList();
    var folderRecordIds = request.Folders.SelectMany(folder => recordRepository.GetRecords(folder.Map())).Except(request.RecordIds).ToList();
    var selected = await syncService.Synchronize(request.RecordIds, peerIds, includeExcluded: true).ConfigureAwait(false);
    var folders = folderRecordIds.Count == 0 ? new SyncResult(0, 0) : await syncService.Synchronize(folderRecordIds, peerIds).ConfigureAwait(false);
    return new SyncResult(selected.Queued + folders.Queued, selected.Skipped + folders.Skipped);
  }

  /// <summary>
  /// Revokes the synchronization of the given own records with the selected or all linked partners. The records are removed
  /// on the partners, kept on this instance and not sent again until they are synchronized manually.
  /// </summary>
  /// <param name="request">Records, folders and partners to revoke the synchronization for.</param>
  /// <returns><see cref="SyncResult"/> with the number of queued removals.</returns>
  [HttpPost("revoke")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<ActionResult<SyncResult>> Revoke([FromBody] SyncRequest request)
  {
    var recordIds = request.RecordIds.Concat(request.Folders.SelectMany(folder => recordRepository.GetRecords(folder.Map())));
    return await syncService.Revoke(recordIds, request.PeerIds?.ToList()).ConfigureAwait(false);
  }

  /// <summary>
  /// Loads the synchronization details of a record: the partners it was sent to or received from and the failed operations.
  /// </summary>
  /// <param name="recordId">Id of the record.</param>
  /// <returns><see cref="RecordSyncInfo"/></returns>
  [HttpGet("records/{recordId:guid}")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<RecordSyncInfo> GetRecordSync(Guid recordId)
  {
    return new RecordSyncInfo
    {
      Links = linkRepository.ListByRecord(recordId),
      Problems = jobRepository.ListProblems(recordId),
      Transfers = jobRepository.ListTransfers(recordId),
      Exclusions = linkRepository.ListExclusions(recordId),
    };
  }
}
