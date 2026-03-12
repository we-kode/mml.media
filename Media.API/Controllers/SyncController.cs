using Asp.Versioning;
using Media.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SyncController : ControllerBase
{
  /// <summary>
  /// The instance name of the running service.
  /// </summary>
  /// <returns>Instance name.</returns>
  [HttpGet("instance")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<string> GetInstance()
  {
    return Env.INSTANCE;
  }
}
