using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
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
  public Records List([FromQuery] string? filter, [FromBody] Contracts.TagFilter tagFilter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
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
    return recordRepository.List(filter, mapper.Map<TagFilter>(tagFilter),!isAdmin, groups, skip, take);
  }

  /// <summary>
  /// Loads a list of artists.
  /// </summary>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("artists")]
  public Artists GetArtists([FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListArtists(skip, take);
  }

  /// <summary>
  /// Loads a list of genres.
  /// </summary>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("genres")]
  public Genres GetGenres([FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListGenres(skip, take);
  }

  /// <summary>
  /// Loads a list of albums.
  /// </summary>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Albums"/></returns>
  [HttpGet("albums")]
  public Albums GetAlbums([FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return recordRepository.ListAlbums(skip, take);
  }
}
