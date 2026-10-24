using Asp.Versioning;
using Media.API.Extensions;
using Media.API.Services;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ArtistController(IArtistRepository artistsRepository, IAuthorizationClient authClient) : ControllerBase
{
  /// <summary>
  /// Loads a list of artists.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of artists.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("artists")]
  public async Task<Artists> GetArtists([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = await HttpContext.ClientGroups(authClient);
    return artistsRepository.List(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of newest artists.
  /// </summary>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("newestArtists")]
  public async Task<Artists> GetNewestArtists()
  {
    var clientGroups = await HttpContext.ClientGroups(authClient);
    return artistsRepository.ListNewest(clientGroups);
  }

  /// <summary>
  /// Loads a list of common artists.
  /// </summary>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("commonArtists")]
  public async Task<Artists> GetCommonArtists()
  {
    var clientGroups = await HttpContext.ClientGroups(authClient);
    return artistsRepository.ListCommon(clientGroups);
  }
}
