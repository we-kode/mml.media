using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Media.API.Middleware;

public class ApiKeyValidator(RequestDelegate next, IConfiguration configuration)
{
  private const string APP_KEY_HEADER = "App-Key";
  private const string ADMIN_APP_KEY = "ADMIN_APP_KEY";
  private const string APP_KEY = "APP_KEY";
  private const string MML_SYNC_HEADER = "MML-Sync-Peer";

  public async Task Invoke(HttpContext context)
  {
    if ((context.Request.Path.Value?.Contains("/snyc", System.StringComparison.InvariantCultureIgnoreCase) ?? false) && context.Request.Headers.TryGetValue(MML_SYNC_HEADER, out var _))
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

public static class ApiKeyValidatorExtension
{
  public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder app)
  {
    app.UseMiddleware<ApiKeyValidator>();
    return app;
  }
}
