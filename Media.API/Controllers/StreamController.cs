using AutoMapper;
using Media.API.Extensions;
using Media.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
  private readonly IMapper mapper;

  public StreamController(IRecordsRepository repository, IMapper mapper)
  {
    this.repository = repository;
    this.mapper = mapper;
  }

  /// <summary>
  /// Streams the given record.
  /// </summary>
  /// <param name="id">Record to be streamed.</param>
  /// <returns>Stream of the given record.</returns>
  /// <response code="404">If record does not exist.</response>
  /// <response code="403">If record is not in group of client.</response>
  [HttpGet("{id:Guid}")]
  public IActionResult Get(Guid id)
  {
    if (!repository.Exists(id))
    {
      return NotFound();
    }

    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    if (!isAdmin && !repository.IsInGroup(id, clientGroups))
    {
      return Forbid();
    }

    var stream = repository.StreamRecord(id);
    return File(stream.Stream, stream.MimeType, true);
  }

  /// <summary>
  /// Returns the next record id in filtered list. 
  /// If repeat is not set, than no id will be returned when record is the last element in list.
  /// In other case the first element will be returned.
  /// If shuffle is set, a random record will be returned.
  /// </summary>
  /// <param name="id">Id of actual playing record.</param>
  /// <param name="tagFilter">The <see cref="Contracts.TagFilter"/> set on the request.</param>
  /// <param name="filter">The title filter set on the request.</param>
  /// <param name="repeat">True, if the records should play endless.</param>
  /// <param name="shuffle">True, if a random record should be loaded next.</param>
  /// <param name="seed">If set, the value will be used for getting next random record.</param>
  /// <returns><see cref="Guid" /> or null if no next record exists.</returns>
  [HttpPost("next/{id:Guid}")]
  public Guid? Next(Guid id, [FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] bool? repeat, [FromQuery] bool? shuffle, [FromQuery] int? seed)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return repository.Next(id, filter, mapper.Map<TagFilter>(tagFilter), !isAdmin, clientGroups, repeat ?? false, shuffle ?? false, seed);
  }

  /// <summary>
  /// Returns the previous record id in filtered list. 
  /// If repeat is not set, than no record will be returned when record is the first element in list.
  /// In other case the last element will be returned.
  /// </summary>
  /// <param name="id">Id of actual playing record.</param>
  /// <param name="tagFilter">The <see cref="Contracts.TagFilter"/> set on the request.</param>
  /// <param name="filter">The title filter set on the request.</param>
  /// <param name="repeat">True, if the records should play endless.</param>
  /// <returns><see cref="Guid" /> or null if no previous record exists.</returns>
  [HttpPost("previous/{id:Guid}")]
  public Guid? Previous(Guid id, [FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] bool? repeat)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return repository.Previous(id, filter, mapper.Map<TagFilter>(tagFilter), !isAdmin, clientGroups, repeat ?? false);
  }

}
