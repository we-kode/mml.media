﻿using AutoMapper;
using Media.API.Contracts;
using Media.Application.Constants;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class RecordController : ControllerBase
{
  private readonly IRecordsRepository recordRepository;
  private readonly IMapper mapper;

  public RecordController(IRecordsRepository recordRepository, IMapper mapper)
  {
    this.recordRepository = recordRepository;
    this.mapper = mapper;
  }

  /// <summary>
  /// Loads a list of existing records.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of records</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Records"/></returns>
  [HttpPost("list")]
  public Records List([FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = (HttpContext.User.GetClaim(Claims.Role) ?? "").Contains("Admin");
    var clientGroups = HttpContext.User.GetClaims("ClientGroup");
    var groups = new List<Guid>();
    foreach (var group in clientGroups)
    {
      if (Guid.TryParse(group, out var id))
      {
        groups.Add(id);
      }
    }
    return recordRepository.List(filter, mapper.Map<Application.Contracts.TagFilter>(tagFilter),!isAdmin, groups, skip, take);
  }

  /// <summary>
  /// Loads a list of artists.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of artists.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("artists")]
  public Artists GetArtists([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListArtists(filter, skip, take);
  }

  /// <summary>
  /// Loads a list of genres.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of genres</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("genres")]
  public Genres GetGenres([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListGenres(filter, skip, take);
  }

  /// <summary>
  /// Loads a list of albums.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of albums.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Albums"/></returns>
  [HttpGet("albums")]
  public Albums GetAlbums([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListAlbums(filter, skip, take);
  }

  /// <summary>
  /// Deletes a list of existing records.
  /// </summary>
  /// <param name="ids">ids of the records to be removed.</param>
  [HttpPost("deleteList")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> DeleteList([FromBody] IList<Guid> ids)
  {
    foreach (var id in ids)
    {
      await recordRepository.DeleteRecord(id).ConfigureAwait(false);
    }

    return Ok();
  }

  /// <summary>
  /// Loads one existing record.
  /// </summary>
  /// <param name="id">id of the record to be loaded.</param>
  /// <returns><see cref="Record"/> of given id</returns>
  /// <response code="404">If record does not exist.</response>
  [HttpGet("{id:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<Record> Get(Guid id)
  {
    if (!recordRepository.Exists(id))
    {
      return NotFound();
    }

    return recordRepository.GetRecord(id);
  }

  /// <summary>
  /// Changes record.
  /// </summary>
  /// <param name="request">New Record to store.</param>
  /// <response code="404">If record does not exists.</response>
  [HttpPost()]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> Post([FromBody] RecordChangeRequest request)
  {
    if (!recordRepository.Exists(request.RecordId))
    {
      return NotFound();
    }

    await recordRepository.Update(mapper.Map<Record>(request)).ConfigureAwait(false);
    return Ok();
  }
}
