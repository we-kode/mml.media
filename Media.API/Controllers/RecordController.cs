using AutoMapper;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.Application.Constants;
using Media.Application.Contracts;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class RecordController : ControllerBase
{
  private readonly IRecordsRepository recordRepository;
  private readonly IMapper mapper;

  public RecordController(IRecordsRepository recordRepository, IMapper mapper)
  {
    this.recordRepository = recordRepository;
    this.mapper = mapper;
  }

  /// <summary>
  /// Loads a list of existing records.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of records</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Records"/></returns>
  [HttpPost("list")]
  public Records List([FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.List(filter, mapper.Map<Application.Contracts.TagFilter>(tagFilter), !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of existing records grouped by folder.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of records</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="RecordFodlers"/></returns>
  [HttpPost("listFolder")]
  public RecordFolders ListFolder([FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.ListFolder(filter, mapper.Map<Application.Contracts.TagFilter>(tagFilter), !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of artists.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of artists.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("artists")]
  public Artists GetArtists([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.ListArtists(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of genres.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of genres</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Artists"/></returns>
  [HttpGet("genres")]
  public Genres GetGenres([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.ListGenres(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Loads a list of albums.
  /// </summary>
  /// <param name="filter">Filter request to filter the list of albums.</param>
  /// <param name="skip">Offset of the list</param>
  /// <param name="take">Size of chunk to be loaded</param>
  /// <returns><see cref="Albums"/></returns>
  [HttpGet("albums")]
  public Albums GetAlbums([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    var isAdmin = HttpContext.IsAdmin();
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.ListAlbums(filter, !isAdmin, clientGroups, skip, take);
  }

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
    return recordRepository.ListLanguages(filter, !isAdmin, clientGroups, skip, take);
  }

  /// <summary>
  /// Deletes a list of existing records.
  /// </summary>
  /// <param name="ids">ids of the records to be removed.</param>
  [HttpPost("deleteList")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> DeleteList([FromBody] IList<Guid> ids)
  {
    foreach (var id in ids)
    {
      await recordRepository.DeleteRecord(id).ConfigureAwait(false);
    }

    return Ok();
  }

  /// <summary>
  /// Deletes a list of record folders.
  /// </summary>
  /// <param name="data"><see cref="RecordFolder"/> to be deleted.</param>
  [HttpPost("deleteFolders")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> DeleteFolders([FromBody] IList<Contracts.RecordFolder> data)
  {
    await recordRepository.DeleteFolders(data.Select(f => mapper.Map<Application.Models.RecordFolder>(f))).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Assigns records to multiple groups.
  /// </summary>
  /// <param name="request">Records to be assigned.</param>
  [HttpPost("assign")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult Assign([FromBody] AssignmentRequest request)
  {
    recordRepository.Assign(request.Items, request.Groups);
    return Ok();
  }

  /// <summary>
  /// Assigns folders to multiple groups.
  /// </summary>
  /// <param name="request">Folders to be assigned.</param>
  [HttpPost("assignFolder")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult AssignFolder([FromBody] AssignFolderRequest request)
  {
    recordRepository.AssignFolder(request.Items.Select(f => mapper.Map<Application.Models.RecordFolder>(f)), request.Groups);
    return Ok();
  }

  /// <summary>
  /// Locks or unlocks records.
  /// </summary>
  /// <param name="request">Records to be locked or unlocked.</param>
  [HttpPost("lock")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult Lock([FromBody] ItemsRequest request)
  {
    recordRepository.Lock(request.Items);
    return Ok();
  }

  /// <summary>
  /// Locks or unlocks items in folders.
  /// </summary>
  /// <param name="request">Folders to be locked or unlocked.</param>
  [HttpPost("lockFolder")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult LockFolder([FromBody] FolderItemsRequest request)
  {
    recordRepository.LockFolder(request.Items.Select(f => mapper.Map<Application.Models.RecordFolder>(f)));
    return Ok();
  }

  /// <summary>
  /// Loads one existing record.
  /// </summary>
  /// <param name="id">id of the record to be loaded.</param>
  /// <returns><see cref="Record"/> of given id</returns>
  /// <response code="404">If record does not exist.</response>
  [HttpGet("{id:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<Record> Get(Guid id)
  {
    if (!recordRepository.Exists(id))
    {
      return NotFound();
    }

    return recordRepository.GetRecord(id);
  }

  /// <summary>
  /// Changes record.
  /// </summary>
  /// <param name="request">New Record to store.</param>
  /// <response code="404">If record does not exists.</response>
  [HttpPost()]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> Post([FromBody] RecordChangeRequest request)
  {
    if (!recordRepository.Exists(request.RecordId))
    {
      return NotFound();
    }

    await recordRepository.Update(mapper.Map<Record>(request)).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Returns available bitrates.
  /// </summary>
  [HttpGet("bitrates")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public GenreBitrates Bitrates()
  {
    return recordRepository.Bitrates();
  }

  /// <summary>
  /// Returns available bitrates.
  /// </summary>
  [HttpPost("bitrates")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult Bitrates([FromBody] List<GenreBitrate> bitrates)
  {
    recordRepository.UpdateBitrates(bitrates);
    return Ok();
  }

  /// <summary>
  /// Removes one bitrate.
  /// </summary>
  [HttpDelete("bitrate/{genreId:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public IActionResult Bitrate(Guid genreId)
  {
    if (!recordRepository.GenreExists(genreId))
    {
      return NotFound();
    }

    recordRepository.DeleteBitrate(genreId);
    return Ok();
  }

  /// <summary>
  /// Loads records of given checksums.
  /// </summary>
  /// <param name="checksums">Checksums of records to be loaded.</param>
  [HttpPost("check")]
  public ActionResult<List<Record>> Check([FromBody] List<string> checksums)
  {
    var clientGroups = HttpContext.ClientGroups();
    return recordRepository.GetRecords(checksums, clientGroups);
  }
}
