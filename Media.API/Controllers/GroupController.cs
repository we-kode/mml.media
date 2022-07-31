using Media.Application.Contracts;
using Media.Application.Constants;
using Media.Application.Models;
using Media.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
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
    /// <param name="request">Filter request to filter the list of groups.</param>
    /// <param name="skip">Offset of the list.</param>
    /// <param name="take">Size of chunk to be loaded.</param>
    /// <returns><see cref="Groups"/></returns>
    [HttpGet()]
    public ActionResult<Groups> List([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
    {
      return _repository.ListGroups(filter, skip, take);
    }
  }
}
