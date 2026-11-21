using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Api.Infrastructure;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/management/reports")]
[Authorize(Roles = "Manager")]
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

    [HttpGet("aging")]
    [ProducesResponseType<ManagementClaimsAgingReportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagementClaimsAgingReportResult>> GetAgingReport(
        [FromQuery] string? marketCode,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        return Ok(await reportingService.GetAgingReportAsync(
            new ManagementClaimsReportQuery(actorStaffUserId, marketCode, fromDate, toDate),
            cancellationToken));
    }

    [HttpGet("performance")]
    [ProducesResponseType<ManagementClaimsPerformanceReportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagementClaimsPerformanceReportResult>> GetPerformanceReport(
        [FromQuery] string? marketCode,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        return Ok(await reportingService.GetPerformanceReportAsync(
            new ManagementClaimsReportQuery(actorStaffUserId, marketCode, fromDate, toDate),
            cancellationToken));
    }

    [HttpGet("sla")]
    [ProducesResponseType<ManagementClaimsSlaReportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagementClaimsSlaReportResult>> GetSlaReport(
        [FromQuery] string? marketCode,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        return Ok(await reportingService.GetSlaReportAsync(
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

        var value = ClaimAccessQueryFactory.GetStaffUserId(User);
        if (value.HasValue)
        {
            staffUserId = value.Value;
            return true;
        }

        return false;
    }
}
