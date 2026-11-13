using Media.Application.Models.Federation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Media.API.Filters;

/// <summary>
/// Converts a <see cref="FederationException"/> into a response containing only the error code.
/// </summary>
public class FederationExceptionFilter : IExceptionFilter
{
  public void OnException(ExceptionContext context)
  {
    if (context.Exception is not FederationException exception)
    {
      return;
    }

    var status = exception.Code switch
    {
      FederationErrors.Disabled or FederationErrors.NotFound => StatusCodes.Status404NotFound,
      FederationErrors.InvalidAssertion => StatusCodes.Status401Unauthorized,
      FederationErrors.InvalidInvitation => StatusCodes.Status403Forbidden,
      FederationErrors.TooManyTransfers => StatusCodes.Status429TooManyRequests,
      FederationErrors.InstanceConflict or FederationErrors.InvalidState => StatusCodes.Status409Conflict,
      FederationErrors.RemoteUnreachable or FederationErrors.RemoteRejected => StatusCodes.Status502BadGateway,
      _ => StatusCodes.Status400BadRequest,
    };

    var detail = exception.Code == FederationErrors.RemoteRejected ? exception.Message : null;
    context.Result = new ObjectResult(new { code = exception.Code, detail }) { StatusCode = status };
    context.ExceptionHandled = true;
  }
}
