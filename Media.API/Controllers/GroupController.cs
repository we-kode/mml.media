using Media.Application.Contracts.Repositories;
using Media.Application.Constants;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using Asp.Versioning;

namespace Media.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
public class GroupController : ControllerBase
{
  private IGroupRepository _repository;

  public GroupController(
    IGroupRepository repository
  )
  {
    _repository = repository;
  }

  /// <summary>
  /// Loads a list of existing groups.
  /// </summary>
  /// <param name="filter">Filter to filter the list of groups.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  /// <returns><see cref="Groups"/></returns>
  [HttpGet()]
  public ActionResult<Groups> List([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return _repository.ListGroups(filter, skip, take);
  }
}
