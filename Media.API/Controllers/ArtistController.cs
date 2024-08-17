using Asp.Versioning;
using Media.API.Extensions;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ArtistController(IArtistRepository artistsRepository) : ControllerBase
{
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
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return artistsRepository.List(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of newest artists.
  /// </summary>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("newestArtists")]
  public Artists GetNewestArtists()
  {
    var clientGroups = HttpContext.ClientGroups();
    return artistsRepository.ListNewest(clientGroups);
  }

  /// <summary>
  /// Loads a list of common artists.
  /// </summary>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("commonArtists")]
  public Artists GetCommonArtists()
  {
    var clientGroups = HttpContext.ClientGroups();
    return artistsRepository.ListCommon(clientGroups);
  }
}
