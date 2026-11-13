using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Services;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
public class ShareRuleController(IShareRuleRepository repository, IShareLinkRevocationQueue revocation) : ControllerBase
{
  /// <summary>
  /// Loads a list of the rules, which allow the clients of groups to share records.
  /// </summary>
  /// <param name="filter">Text, which must be part of a group, genre, album or artist name of the rule.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  [HttpGet]
  public ShareRules List([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return repository.List(filter, skip, take);
  }

  /// <summary>
  /// Loads a rule.
  /// </summary>
  /// <param name="id">Id of the rule.</param>
  /// <response code="404">If the rule does not exist.</response>
  [HttpGet("{id:guid}")]
  public ActionResult<ShareRule> Get(Guid id)
  {
    var rule = repository.Load(id);
    return rule == null ? NotFound() : rule;
  }

  /// <summary>
  /// Creates a rule or changes the rule with the id of the request. Share links of clients, who may not share all their records anymore,
  /// are revoked in the background.
  /// </summary>
  /// <param name="request">The rule. It matches all records or needs at least one genre, album or artist. A rule without group allows nobody to
  /// share.</param>
  /// <response code="400">If the rule matches neither all records nor has a criterion.</response>
  /// <response code="404">If the rule to be changed does not exist.</response>
  [HttpPost]
  public async Task<IActionResult> Save([FromBody] ShareRuleRequest request)
  {
    if (!request.AllRecords && request.Genres.Count + request.Albums.Count + request.Artists.Count == 0)
    {
      return BadRequest();
    }

    var saved = await repository.Save(request.Id, request.Groups, request.AllRecords, request.Genres, request.Albums, request.Artists).ConfigureAwait(false);
    if (!saved)
    {
      return NotFound();
    }

    revocation.Enqueue();
    return Ok();
  }

  /// <summary>
  /// Deletes rules. Share links of clients, who may not share all their records anymore, are revoked in the background.
  /// </summary>
  /// <param name="ids">Ids of the rules to be deleted.</param>
  [HttpPost("deleteList")]
  public async Task<IActionResult> DeleteList([FromBody] IList<Guid> ids)
  {
    await repository.Delete(ids).ConfigureAwait(false);
    revocation.Enqueue();
    return Ok();
  }
}
