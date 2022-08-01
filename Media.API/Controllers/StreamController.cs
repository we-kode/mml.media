using Media.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class StreamController : ControllerBase
{

  private readonly IRecordsRepository repository;

  public StreamController(IRecordsRepository repository)
  {
    this.repository = repository;
  }

  /// <summary>
  /// Streams the given record.
  /// </summary>
  /// <param name="id">Record to be streamed.</param>
  /// <returns>Stream of the given record.</returns>
  /// <response code="404">If record does not exist.</response>
  [HttpGet("{id:Guid}")]
  public IActionResult Get(Guid id)
  {
    if (!repository.Exists(id))
    {
      return NotFound();
    }
    var stream = repository.StreamRecord(id);
    return File(stream.Stream, stream.MimeType, true);
  }
}
