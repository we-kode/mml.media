using AutoMapper;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeDetective.Storage.Xml.v2;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InfoController : ControllerBase
{

  private readonly IInfoRepository repository;

  public InfoController(IInfoRepository repository)
  {
    this.repository = repository;
  }

  /// <summary>
  /// Loads a list files and directories of given path.
  /// </summary>
  /// <param name="path">Folder to be loaded</param>
  /// <returns><see cref="Infos"/></returns>
  /// <response code="404">If path does not exists.</response>
  [HttpGet]
  public ActionResult<Infos> Info([FromQuery] string? path, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    if (!string.IsNullOrEmpty(path) && !repository.ExistsFolder(path))
    {
      return NotFound();
    }

    return repository.List(path, skip, take);
  }

  /// <summary>
  /// Loads the content of the given path.
  /// </summary>
  /// <param name="path">Path where the content should be loaded from.</param>
  /// <returns>Conent of file.</returns>
  /// <response code="404">If path does not exists.</response>
  [HttpGet("content")]
  public ActionResult<string> FileContent([FromQuery] string path)
  {
    if (string.IsNullOrEmpty(path) || !repository.ExistsFile(path))
    {
      return NotFound();
    }

    return repository.Content(path);
  }
}
