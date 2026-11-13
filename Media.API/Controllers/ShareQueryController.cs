using Asp.Versioning;
using Media.API.Contracts;
using Media.API.Share;
using Media.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/media/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ShareQueryController(IShareQueryRepository repository) : ControllerBase
{
  private const int MaxValues = 1000;
  private const int MaxValueLength = 500;

  /// <summary>
  /// Returns a short token for the filters of a search, so the search can be shared with a short link.
  /// </summary>
  /// <param name="search">The filters of the search.</param>
  /// <response code="400">If the filters are empty or contain more than 1000 values or values longer than 500 characters.</response>
  [HttpPost]
  public async Task<ActionResult<ShareQueryResponse>> Create([FromBody] ShareSearch search)
  {
    var canonical = Canonical(search);
    if (canonical == null)
    {
      return BadRequest();
    }

    var json = JsonSerializer.Serialize(canonical);
    var token = await repository.GetOrCreate(ShareTokens.Hash(json), json, ShareTokens.Create(12), DateTime.UtcNow).ConfigureAwait(false);
    return new ShareQueryResponse(token);
  }

  /// <summary>
  /// Returns the filters of a shared search.
  /// </summary>
  /// <param name="token">The token of the link.</param>
  /// <response code="404">If the token is unknown or was not used for a long time.</response>
  [HttpGet("{token}")]
  public ActionResult<ShareSearch> Get(string token)
  {
    var json = repository.Find(token, DateTime.UtcNow);
    try
    {
      return json == null ? NotFound() : JsonSerializer.Deserialize<ShareSearch>(json)!;
    }
    catch (JsonException)
    {
      return NotFound();
    }
  }

  // returns the filters with sorted values, so equal searches get the same token, or null if they are invalid.
  private static ShareSearch? Canonical(ShareSearch search)
  {
    List<List<string>> lists = [search.Artist, search.ArtistName, search.Genre, search.GenreName, search.Album, search.AlbumName, search.Language];
    var values = lists.SelectMany(l => l).Concat(new[] { search.Q, search.From, search.To }.OfType<string>()).ToList();
    if (values.Count == 0 || values.Count > MaxValues || values.Any(v => v.Length > MaxValueLength))
    {
      return null;
    }

    static List<string> Sorted(List<string> values) => [.. values.Order(StringComparer.Ordinal)];
    return new ShareSearch
    {
      Q = search.Q,
      Artist = Sorted(search.Artist),
      ArtistName = Sorted(search.ArtistName),
      Genre = Sorted(search.Genre),
      GenreName = Sorted(search.GenreName),
      Album = Sorted(search.Album),
      AlbumName = Sorted(search.AlbumName),
      Language = Sorted(search.Language),
      From = search.From,
      To = search.To,
    };
  }
}
