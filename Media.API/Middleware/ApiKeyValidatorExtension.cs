using Microsoft.AspNetCore.Builder;

namespace Media.API.Middleware;

public static class ApiKeyValidatorExtension
{
  public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder app)
  {
    app.UseMiddleware<ApiKeyValidator>();
    return app;
  }
}
