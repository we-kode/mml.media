using Asp.Versioning;
using Media.API.Services;
using Media.API.Share;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Media.API.Controllers;

/// <summary>
/// Public pages to open shared content and to download shared records without login.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiVersionNeutral]
[Route("media/s")]
public class ShareDownloadController(IShareLinkRepository linkRepository, IShareArchiveService archive, IShareLinkRevoker revoker, SharePages pages) : ControllerBase
{
  /// <summary>
  /// Opens the app with the content of a shared search or record.
  /// </summary>
  [HttpGet("open")]
  public ContentResult OpenApp()
  {
    return Html(pages.Open());
  }

  /// <summary>
  /// Shows what a share link contains. The link is not used by opening the page, so link previews of messengers do not use it up. A link
  /// is revoked, if its creator may not share all of its records anymore.
  /// </summary>
  /// <param name="token">Secret token of the link.</param>
  [HttpGet("download/{token}")]
  public async Task<ContentResult> Preview(string token)
  {
    var link = linkRepository.Find(ShareTokens.Hash(token), DateTime.UtcNow);
    if (link == null || !await revoker.KeepIfAllowed(link).ConfigureAwait(false))
    {
      return Html(pages.Invalid(), StatusCodes.Status410Gone);
    }

    return Html(pages.Download(link.RecordIds.Count, link.SingleUse, link.ExpiresAt));
  }

  /// <summary>
  /// Downloads the records of a share link: a single record as audio file, multiple records as zip archive. Files are named by their titles.
  /// </summary>
  /// <param name="token">Secret token of the link.</param>
  [HttpPost("download/{token}")]
  public async Task<IActionResult> Download(string token)
  {
    var now = DateTime.UtcNow;
    var link = linkRepository.Find(ShareTokens.Hash(token), now);
    if (link == null || !await revoker.KeepIfAllowed(link).ConfigureAwait(false) || !await linkRepository.TryUse(link.Id, now))
    {
      return Html(pages.Invalid(), StatusCodes.Status410Gone);
    }

    if (link.RecordIds.Count == 1)
    {
      var stream = archive.Open(link.RecordIds[0]);
      return stream == null ? NotFound() : File(stream.Stream, stream.MimeType, ShareFileNames.Create(stream, new HashSet<string>()));
    }

    var bodyControl = HttpContext.Features.Get<IHttpBodyControlFeature>();
    bodyControl?.AllowSynchronousIO = true;

    Response.ContentType = "application/zip";
    var disposition = new ContentDispositionHeaderValue("attachment");
    disposition.SetHttpFileName(string.Create(CultureInfo.InvariantCulture, $"Records_{now:yyyy-MM-dd}.zip"));
    Response.Headers.ContentDisposition = disposition.ToString();
    await archive.WriteZip(link.RecordIds, Response.Body, HttpContext.RequestAborted).ConfigureAwait(false);
    return new EmptyResult();
  }

  private ContentResult Html(string html, int status = StatusCodes.Status200OK)
  {
    Response.Headers.CacheControl = "no-store";
    return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = status };
  }
}
