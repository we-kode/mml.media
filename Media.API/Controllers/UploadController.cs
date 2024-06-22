using Asp.Versioning;
using MassTransit;
using Media.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeDetective;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Application.Constants.Roles.Admin)]
public class UploadController(IPublishEndpoint publishEndpoint) : ControllerBase
{
  private readonly ContentInspector inspector = new ContentInspectorBuilder()
  {
    Definitions = MimeDetective.Definitions.Default.FileTypes.Audio.MP3(),
    Parallel = true
  }.Build();

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

      if (file.Length > 0)
      {

        using var readStream = file.OpenReadStream();
        var mimeResult = inspector.Inspect(readStream);
        var resultsByMimeType = mimeResult.ByMimeType();
        readStream.Close();

        if (!resultsByMimeType.Any(result => result.MimeType == "audio/mpeg3"))
        {
          return BadRequest("INVALID_FORMAT_MP3");
        }

        var fileName = file.FileName;
        var fullPath = Path.Combine(pathToSave, file.FileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
          file.CopyTo(stream);
        }

        // determine groups
        var groups = new List<Guid>();
        if (Request.Form.Keys.Any(key => key == "Groups"))
        {
          foreach (var group in Request.Form["Groups"])
          {
            if (group != null)
            {
              groups.Add(Guid.Parse(group));
            }
          }
        }

        // publish message to start compressing file and indexing.
        await publishEndpoint.Publish<FileUploaded>(new
        {
          FileName = fileName,
          Date = DateTime.Parse(Request.Form["LastModifiedDate"]!),
          Groups = groups
        }).ConfigureAwait(false);

        return Ok();
      }
      else
      {
        return BadRequest("INVALID_FILE_EMPTY");
      }
    }
    catch (Exception)
    {
      return StatusCode(500);
    }
  }
}
