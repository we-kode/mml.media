using Media.API.Services;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Media.API.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
  /// <summary>
  /// Returns, if user contains admin claim.
  /// </summary>
  /// <param name="context"><see cref="HttpContext"/></param>
  /// <returns>True, if user has admin claim.</returns>
  public static bool IsAdmin(this HttpContext context)
  {
    return (context.User.GetClaim(Claims.Role) ?? "").Contains("Admin");
  }

  /// <summary>
  /// Returns all client groups contained in user claim.
  /// </summary>
  /// <param name="context"><see cref="HttpContext"/></param>
  /// <returns><see cref="IEnumerable{T}"/> of <see cref="Guid"/></returns>
  public static async Task<IList<Guid>> ClientGroups(this HttpContext context, IAuthorizationClient authorization)
  {
    if (context.IsAdmin())
    {
      return [];
    }

    var clientId = context.User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(clientId))
    {
      return [];
    }

    return await authorization.GetGroups("bf8854b2-5174-4ca1-ac10-cb11ba4ff053");
  }
}
