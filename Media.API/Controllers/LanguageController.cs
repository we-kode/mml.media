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
public class LanguageController(ILanguageRepository languageRepository) : ControllerBase
{
  /// <summary>
  /// Loads a list of languages.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of albums.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Languages"/></returns>
  [HttpGet("languages")]
  public Languages GetLanguages([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return languageRepository.ListLanguages(filter, !isAdmin, clientGroups, skip, take);
  }
}
