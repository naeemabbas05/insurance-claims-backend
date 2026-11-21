using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/status")]
public sealed class StatusController(IOptions<ServiceOptions> serviceOptions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus() =>
        Ok(new
        {
            service = serviceOptions.Value.Name,
            region = serviceOptions.Value.Region,
            status = "running"
        });
}
