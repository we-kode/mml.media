using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media/[controller]")]
public class UploadController : ControllerBase
{
  [HttpGet]
  [AllowAnonymous]
  public string Get()
  {
    return "Hello Media";
  }
}
