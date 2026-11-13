using Asp.Versioning;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Application.Constants.Roles.Admin)]
public class UploadController(IIndexService indexService, IAudioInspector audioInspector) : ControllerBase
{
  /// <summary>
  /// Uploads one file to the tmp folder. The maximum file size is limited by the reverse proxy.
  /// </summary>
  /// <response code="400">If wrong or no file is uploaded.</response>
  /// <response code="500">If error on uploading file.</response>
  [HttpPost()]
  [DisableRequestSizeLimit]
  [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
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
        var isMp3 = audioInspector.IsMp3(readStream);
        readStream.Close();

        if (!isMp3)
        {
          return BadRequest("INVALID_FORMAT_MP3");
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == "..")
        {
          return BadRequest("INVALID_FILE_NAME");
        }

        var fullPath = Path.Combine(pathToSave, fileName);
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

        // index file and extract metadata from it.
        _ = indexService.IndexFile(fileName, DateTime.Parse(Request.Form["LastModifiedDate"]!), groups);

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
