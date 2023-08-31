using AutoMapper;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.Application.Constants;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using OpenIddict.Validation.AspNetCore;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class LivestreamController : ControllerBase
{

  private readonly ILivestreamRepository repository;
  private readonly IMapper mapper;
  static Lazy<HttpClient> client = new Lazy<HttpClient>();

  public LivestreamController(ILivestreamRepository repository, IMapper mapper)
  {
    this.repository = repository;
    this.mapper = mapper;
  }

  /// <summary>
  /// Loads a list of existing livestreams.
  /// </summary>
  /// <param name="filter">Filter request to filter the list</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Records"/></returns>
  [HttpPost("list")]
  public Livestreams List([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return repository.List(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Deletes a list of existing livestream.
  /// </summary>
  /// <param name="ids">ids of the livestreams to be removed.</param>
  [HttpPost("deleteList")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> DeleteList([FromBody] IList<Guid> ids)
  {
    foreach (var id in ids)
    {
      await repository.Delete(id).ConfigureAwait(false);
    }

    return Ok();
  }

  /// <summary>
  /// Loads one existing livestream.
  /// </summary>
  /// <param name="id">id of the livestream to be loaded.</param>
  /// <returns><see cref="Record"/> of given id</returns>
  /// <response code="404">If livestream does not exist.</response>
  [HttpGet("{id:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<LivestreamSettings> Get(Guid id)
  {
    if (!repository.Exists(id))
    {
      return NotFound();
    }

    return repository.Load(id);
  }

  /// <summary>
  /// Changes livestream settings.
  /// </summary>
  /// <param name="request">New livestream to store.</param>
  /// <response code="404">If livestream does not exists.</response>
  [HttpPost()]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> Post([FromBody] LivestreamChangeRequest request)
  {
    if (request.RecordId.HasValue && !repository.Exists(request.RecordId.Value))
    {
      return NotFound();
    }

    await repository.Update(mapper.Map<LivestreamSettings>(request)).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Streams the given livestream.
  /// </summary>
  /// <param name="id">Livestream to be streamed.</param>
  /// <returns>Stream of the given record.</returns>
  /// <response code="404">If record does not exist.</response>
  /// <response code="403">If record is not in group of client.</response>
  [HttpGet("stream/{id:Guid}")]
  public async Task<IActionResult> Stream(Guid id)
  {
    if (!repository.Exists(id))
    {
      return NotFound();
    }

    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    if (!isAdmin && !repository.IsInGroup(id, clientGroups))
    {
      return Forbid();
    }

    try
    {
      var streamUrl = repository.Stream(id);
      Stream stream = await client.Value.GetStreamAsync(streamUrl);
      var result = new FileStreamResult(stream, "audio/mpeg3")
      {
        EnableRangeProcessing = true
      };
      return result;
    }
    catch (Exception)
    {
      return NotFound();
    }
  }

}
