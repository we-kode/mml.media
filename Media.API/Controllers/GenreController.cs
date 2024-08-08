using Asp.Versioning;
using Media.API.Extensions;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class GenreController(IGenreRepository genresRepository) : ControllerBase
{
  /// <summary>
  /// Loads a list of genres.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of genres</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Genres"/></returns>
  [HttpGet("genres")]
  public Genres GetGenres([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return genresRepository.List(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of common artists.
  /// </summary>
  /// <returns><see cref="Genres"/></returns>
  [HttpGet("commonGenres")]
  public Genres GetCommonGenres()
  {
    var clientGroups = HttpContext.ClientGroups();
    return genresRepository.ListCommon(clientGroups);
  }

  /// <summary>
  /// Returns available bitrates.
  /// </summary>
  [HttpGet("bitrates")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public GenreBitrates Bitrates()
  {
    return genresRepository.Bitrates();
  }

  /// <summary>
  /// Returns available bitrates.
  /// </summary>
  [HttpPost("bitrates")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> Bitrates([FromBody] List<GenreBitrate> bitrates)
  {
    await genresRepository.UpdateBitrates(bitrates);
    return Ok();
  }

  /// <summary>
  /// Removes one bitrate.
  /// </summary>
  [HttpDelete("bitrate/{genreId:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult Bitrate(Guid genreId)
  {
    if (!genresRepository.Exists(genreId))
    {
      return NotFound();
    }

    genresRepository.DeleteBitrate(genreId);
    return Ok();
  }
}
