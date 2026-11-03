using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.API.Services;
using Media.API.Share;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.Application.Models.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ShareController(
  IRecordRepository recordRepository,
  IShareRuleRepository ruleRepository,
  IShareLinkRepository linkRepository,
  ISettingRepository settingRepository,
  IAuthorizationClient authClient,
  IOptions<ShareOptions> options) : ControllerBase
{
  // upper bound of requested records and records of folders, that are checked at once.
  private const int MaxRequested = 20000;

  /// <summary>
  /// Returns how many of the requested records and folder records the requester is allowed to share.
  /// </summary>
  /// <param name="request">Records and folders to be checked.</param>
  /// <response code="400">If nothing was requested.</response>
  [HttpPost("check")]
  public async Task<ActionResult<ShareCheckResponse>> Check([FromBody] ShareRequest request)
  {
    var requested = Requested(request);
    if (requested == null)
    {
      return BadRequest();
    }

    var shareable = await Shareable(requested);
    var explicitIds = request.Records.ToHashSet();
    return new ShareCheckResponse(
      shareable.Count,
      requested.Count,
      options.Value.MaxRecordsPerLink,
      [.. shareable.Where(explicitIds.Contains)]);
  }

  /// <summary>
  /// Returns true, if the requester is allowed to share at least some records.
  /// </summary>
  [HttpGet("capability")]
  public async Task<bool> Capability()
  {
    return HttpContext.IsAdmin() || ruleRepository.HasRules(await HttpContext.ClientGroups(authClient));
  }

  /// <summary>
  /// Returns the settings, which control the links of clients.
  /// </summary>
  [HttpGet("settings")]
  public ShareSettings GetSettings()
  {
    return settingRepository.LoadShareSettings();
  }

  /// <summary>
  /// Saves the settings, which control the links of clients and how long shared searches are kept.
  /// </summary>
  /// <param name="settings">The settings.</param>
  [HttpPost("settings")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult SaveSettings([FromBody] ShareSettingsRequest settings)
  {
    settingRepository.SaveShareSettings(new ShareSettings
    {
      UserSettingsAllowed = settings.UserSettingsAllowed,
      UserValidForHours = settings.UserValidForHours,
      UserSingleUse = settings.UserSingleUse,
      SearchLinkRetentionDays = settings.SearchLinkRetentionDays,
    });
    return Ok();
  }

  /// <summary>
  /// Creates a link, to download the given records without login. Records the requester must not share are skipped.
  /// </summary>
  /// <param name="request">Records and folders to be shared.</param>
  /// <response code="400">If nothing to share was requested, more records than allowed per link can be shared or the validity is invalid.</response>
  /// <response code="403">If the requester must not share any of the records.</response>
  [HttpPost]
  public async Task<ActionResult<ShareResponse>> Create([FromBody] ShareRequest request)
  {
    var requested = Requested(request);
    if (requested == null)
    {
      return BadRequest();
    }

    var shareable = await Shareable(requested);
    if (shareable.Count == 0)
    {
      return HttpContext.IsAdmin() ? BadRequest() : Forbid();
    }

    if (shareable.Count > options.Value.MaxRecordsPerLink)
    {
      return BadRequest();
    }

    var settings = GetSettings();
    var userDecides = HttpContext.IsAdmin() || settings.UserSettingsAllowed;
    var hours = userDecides ? request.ValidForHours ?? settings.UserValidForHours : settings.UserValidForHours;
    var singleUse = userDecides ? request.SingleUse ?? settings.UserSingleUse : settings.UserSingleUse;
    if (hours < 1 || hours > ShareSettings.MaxValidForHours)
    {
      return BadRequest();
    }

    var token = ShareTokens.Create();
    await linkRepository.Create(ShareTokens.Hash(token), DateTime.UtcNow.AddHours(hours), singleUse, Subject(), HttpContext.IsAdmin(), shareable);
    return new ShareResponse(token, shareable.Count, requested.Count - shareable.Count);
  }

  /// <summary>
  /// Returns the active links of the requester.
  /// </summary>
  [HttpGet("links")]
  public IList<ShareLinkInfo> GetLinks()
  {
    return linkRepository.GetActive(Subject(), DateTime.UtcNow);
  }

  /// <summary>
  /// Deletes a link of the requester, so it can not be used anymore.
  /// </summary>
  /// <param name="id">Id of the link.</param>
  /// <response code="404">If the requester has no link with this id.</response>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> DeleteLink(Guid id)
  {
    return await linkRepository.Delete(id, Subject()).ConfigureAwait(false) ? Ok() : NotFound();
  }

  private string Subject()
  {
    return HttpContext.User.FindFirst("sub")?.Value ?? string.Empty;
  }

  // returns the distinct ids of the requested records and the records of the requested folders or null, if the request is invalid.
  private List<Guid>? Requested(ShareRequest request)
  {
    var requested = new List<Guid>(request.Records);
    try
    {
      foreach (var folder in request.Folders)
      {
        requested.AddRange(recordRepository.GetRecords(folder.Map()));
      }
    }
    catch (ArgumentOutOfRangeException)
    {
      return null;
    }

    requested = [.. requested.Distinct()];
    return requested.Count == 0 || requested.Count > MaxRequested ? null : requested;
  }

  private async Task<IList<Guid>> Shareable(IEnumerable<Guid> ids)
  {
    if (HttpContext.IsAdmin())
    {
      return recordRepository.GetExistingIds(ids);
    }

    return ruleRepository.FilterShareable(ids, await HttpContext.ClientGroups(authClient));
  }
}
