using Media.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = "Admin")]
public class SettingsController : ControllerBase
{

  private readonly ISettingsRepository settingsRepository;

  public SettingsController(ISettingsRepository settingsRepository)
  {
    this.settingsRepository = settingsRepository;
  }

  /// <summary>
  /// Retuns the saved value.
  /// </summary>
  /// <returns>Saved compression rate.</returns>
  [HttpGet()]
  public string compressionRate([FromQuery] string key)
  {
    return settingsRepository.Get(key, string.Empty);
  }

  /// <summary>
  /// Updates the saved setting.
  /// </summary>
  /// <param name="key">The key.</param>
  /// <param name="value">The value to be saved.</param>
  [HttpPost()]
  public IActionResult compressionRate([FromQuery] string key, [FromQuery] string value)
  {
    settingsRepository.Save(key, value);
    return Ok();
  }
}
