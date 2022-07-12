using MassTransit;
using Media.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = "Admin")]
public class UploadController : ControllerBase
{
  private readonly IPublishEndpoint _publishEndpoint;

  public UploadController(IPublishEndpoint publishEndpoint)
  {
    _publishEndpoint = publishEndpoint;
  }

  /// <summary>
  /// Uploads one file to the tmp folder.
  /// </summary>
  /// <response code="400">If wrong or no file is uploaded.</response>
  /// <response code="500">If error on uploading file.</response>
  [HttpPost()]
  [DisableRequestSizeLimit]
  public async Task<IActionResult> Post()
  {
    try
    {
      var file = Request.Form.Files[0];
      var pathToSave = Path.Combine("/tmp/records");
      Directory.CreateDirectory(pathToSave);

      // if file is not mp3 bad request. Won't uploading it.
      if (!file.FileName.EndsWith(".mp3"))
      {
        return BadRequest();
      }

      if (file.Length > 0)
      {

        var fileName = file.FileName;
        var fullPath = Path.Combine(pathToSave, file.FileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
          file.CopyTo(stream);
        }

        // publish message to start compressing file and indexing.
        await _publishEndpoint.Publish<FileUploaded>(new
        {
          FileName = fileName,
          Date = DateTime.Parse(Request.Form["LastModifiedDate"])
        }).ConfigureAwait(false);

        return Ok();
      }
      else
      {
        return BadRequest();
      }
    }
    catch (Exception)
    {
      return StatusCode(500);
    }
  }
}
