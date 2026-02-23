using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Media.API.Filters;

public class RemoveVersionParameterFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var versionParameter = operation?.Parameters?.Single(p => p.Name == "version");
    if (versionParameter != null)
    {
      operation!.Parameters!.Remove(versionParameter);
    }
  }
}
