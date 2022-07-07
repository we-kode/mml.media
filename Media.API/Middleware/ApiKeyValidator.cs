using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Media.Middleware;

public class ApiKeyValidator
{
  private readonly RequestDelegate _next;
  private readonly IConfiguration _configuration;

  private const string APP_KEY_HEADER = "App-Key";
  private const string ADMIN_APP_KEY = "ADMIN_APP_KEY";
  private const string APP_KEY = "APP_KEY";

  public ApiKeyValidator(RequestDelegate next, IConfiguration configuration)
  {
    _next = next;
    _configuration = configuration;
  }

  public async Task Invoke(HttpContext context)
  {
    if (!context.Request.Headers.ContainsKey(APP_KEY_HEADER))
    {
      await _UnauthorizedRespone(context);
      return;
    }

    var isAdminAppRequest = context.Request.Headers[APP_KEY_HEADER] == _configuration.GetValue(ADMIN_APP_KEY, string.Empty);
    var isAppRequest = context.Request.Headers[APP_KEY_HEADER] == _configuration.GetValue(APP_KEY, string.Empty);
    if (!isAdminAppRequest && !isAppRequest)
    {
      await _UnauthorizedRespone(context);
      return;
    }

    await _next.Invoke(context);
  }

  private async Task _UnauthorizedRespone(HttpContext context)
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
