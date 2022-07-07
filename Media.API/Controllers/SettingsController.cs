using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
//[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = "Admin")]
public class SettingsController : ControllerBase
{
  /// <summary>
  /// Retuns the saved compression rate in kbit/s.
  /// </summary>
  /// <returns>Saved compression rate.</returns>
  [HttpGet("compressionRate")]
  public string compressionRate()
  {
    return "Hello Media";
  }

  /// <summary>
  /// Updates the saved compression rate in kbit/s.
  /// </summary>
  /// <param name="compressRate">The new compression rate in kbit/s.</param>
  /// <returns></returns>
  [HttpPost("compressionRate")]
  public IActionResult compressionRate([FromQuery] string compressRate)
  {
    return Ok();
  }
}
