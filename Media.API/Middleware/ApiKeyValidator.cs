using Media.Application.Models.Federation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Media.API.Middleware;

public class ApiKeyValidator(RequestDelegate next, IConfiguration configuration)
{
  private const string APP_KEY_HEADER = "App-Key";
  private const string ADMIN_APP_KEY = "ADMIN_APP_KEY";
  private const string APP_KEY = "APP_KEY";
  // partner instances authenticate with signed assertions instead of an app key.
  private static readonly PathString FederationPeerPath = new(FederationOptions.EndpointPath);

  public async Task Invoke(HttpContext context)
  {
    if (context.Request.Path.StartsWithSegments(FederationPeerPath, System.StringComparison.OrdinalIgnoreCase))
    {
      await next.Invoke(context);
      return;
    }

    if (!context.Request.Headers.TryGetValue(APP_KEY_HEADER, out Microsoft.Extensions.Primitives.StringValues value))
    {
      await UnauthorizedRespone(context);
      return;
    }

    var isAdminAppRequest = value == configuration.GetValue(ADMIN_APP_KEY, string.Empty);
    var isAppRequest = value == configuration.GetValue(APP_KEY, string.Empty);
    if (!isAdminAppRequest && !isAppRequest)
    {
      await UnauthorizedRespone(context);
      return;
    }

    await next.Invoke(context);
  }

  private static async Task UnauthorizedRespone(HttpContext context)
  {
    context.Response.StatusCode = 401; //Unauthorized               
    await context.Response.WriteAsync(string.Empty);
  }
}
