using System.Security.Claims;
using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/management/reports")]
public sealed class ManagementReportingController(
    IManagementClaimsReportingService reportingService) : ControllerBase
{
    [HttpGet("claims")]
    [ProducesResponseType<ManagementClaimsReportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagementClaimsReportResult>> GetClaimsReport(
        [FromQuery] string? marketCode,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        return Ok(await reportingService.GetClaimsReportAsync(
            new ManagementClaimsReportQuery(actorStaffUserId, marketCode, fromDate, toDate),
            cancellationToken));
    }

    private bool TryGetStaffUserId(out Guid staffUserId)
    {
        staffUserId = Guid.Empty;
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var value = User.FindFirstValue("staff_user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out staffUserId) && staffUserId != Guid.Empty;
    }
}
