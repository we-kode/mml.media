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
  /// Sends the given own records to every linked partner with a matching outgoing rule, if they are not sent already.
  /// </summary>
  /// <param name="request">Records and folders to synchronize.</param>
  /// <returns><see cref="SyncResult"/></returns>
  [HttpPost]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<ActionResult<SyncResult>> Synchronize([FromBody] SyncRequest request)
  {
    var recordIds = request.RecordIds.Concat(request.Folders.SelectMany(folder => recordRepository.GetRecords(folder.Map())));
    return await syncService.Synchronize(recordIds).ConfigureAwait(false);
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
    };
  }
}
