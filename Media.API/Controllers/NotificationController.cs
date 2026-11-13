using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Extensions;
using Media.API.Services;
using Media.Application.Constants;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class NotificationController(INotificationRepository repository, IAuthorizationClient authClient) : ControllerBase
{
  /// <summary>
  /// Loads the notifications currently valid for the calling client. The targets of the notifications are not returned.
  /// </summary>
  /// <returns>The active notifications ordered by severity and start of the validity.</returns>
  [HttpGet("active")]
  public async Task<IList<Notification>> Active()
  {
    var clientId = HttpContext.User.FindFirst("sub")?.Value ?? string.Empty;
    var clientGroups = await HttpContext.ClientGroups(authClient);
    var notifications = repository.Active(clientId, clientGroups, DateTime.UtcNow);
    foreach (var notification in notifications)
    {
      notification.ClientIds = [];
      notification.Groups = [];
    }

    return notifications;
  }

  /// <summary>
  /// Loads a list of notifications.
  /// </summary>
  /// <param name="filter">Filter of the title or message.</param>
  /// <param name="skip">Offset of the list.</param>
  /// <param name="take">Size of chunk to be loaded.</param>
  [HttpGet("list")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public Notifications List([FromQuery] string? filter, [FromQuery] int skip = Application.Constants.List.Skip, [FromQuery] int take = Application.Constants.List.Take)
  {
    return repository.List(filter, skip, take);
  }

  /// <summary>
  /// Loads one notification.
  /// </summary>
  /// <param name="id">Id of the notification.</param>
  /// <response code="404">If the notification does not exist.</response>
  [HttpGet("{id:Guid}")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public ActionResult<Notification> Get(Guid id)
  {
    var notification = repository.Load(id);
    return notification == null ? NotFound() : notification;
  }

  /// <summary>
  /// Creates or changes a notification.
  /// </summary>
  /// <param name="request">The notification to store.</param>
  /// <response code="400">If the end of the validity is not after its start.</response>
  /// <response code="404">If the notification to change does not exist.</response>
  [HttpPost]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> Post([FromBody] NotificationChangeRequest request)
  {
    if (request.ValidFrom.HasValue && request.ValidTo.HasValue && request.ValidFrom >= request.ValidTo)
    {
      return BadRequest();
    }

    if (request.NotificationId.HasValue && !repository.Exists(request.NotificationId.Value))
    {
      return NotFound();
    }

    await repository.Save(new Notification
    {
      NotificationId = request.NotificationId ?? Guid.NewGuid(),
      Title = request.Title,
      Message = request.Message,
      Severity = request.Severity,
      ValidFrom = request.ValidFrom,
      ValidTo = request.ValidTo,
      ClientIds = request.ClientIds,
      Groups = request.Groups,
    }).ConfigureAwait(false);
    return Ok();
  }

  /// <summary>
  /// Deletes notifications.
  /// </summary>
  /// <param name="ids">Ids of the notifications to delete.</param>
  [HttpPost("deleteList")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public async Task<IActionResult> DeleteList([FromBody] IList<Guid> ids)
  {
    await repository.Delete(ids).ConfigureAwait(false);
    return Ok();
  }
}
