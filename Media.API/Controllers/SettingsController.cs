using AutoMapper;
using Media.API.Contracts;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Generic;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Application.Constants.Roles.Admin)]
public class SettingsController : ControllerBase
{

  private readonly ISettingsRepository settingsRepository;
  private readonly IMapper mapper;

  public SettingsController(ISettingsRepository settingsRepository, IMapper mapper)
  {
    this.settingsRepository = settingsRepository;
    this.mapper = mapper;
  }

  /// <summary>
  /// Returns available settings.
  /// </summary>
  [HttpGet]
  public Settings Get()
  {
    return settingsRepository.Get();
  }

  /// <summary>
  /// Saves the settings.
  /// </summary>
  /// <param name="settings">Settings map.</param>
  [HttpPost]
  public IActionResult Post([FromBody] SettingsRequest settings)
  {
    settingsRepository.Save(mapper.Map<Settings>(settings));
    return Ok();
  }
}
