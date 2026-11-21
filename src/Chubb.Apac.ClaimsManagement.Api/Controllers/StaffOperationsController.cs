using System.Security.Claims;
using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Api.Infrastructure;
using Chubb.Apac.ClaimsManagement.Api.Models;
using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/staff")]
[Authorize(Roles = "ClaimsOfficer,Manager")]
public sealed class StaffOperationsController(
    IClaimStaffOperationsService staffOperationsService,
    IClaimAccessAuthorizationService claimAccessAuthorizationService) : ControllerBase
{
    [HttpGet("claims/incoming")]
    [ProducesResponseType<PagedResult<StaffClaimListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StaffClaimListItem>>> GetIncomingClaims(
        [FromQuery] string marketCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeStaffAsync(
            actorStaffUserId,
            ClaimAccessQueryFactory.GetAccessRole(User),
            marketCode,
            cancellationToken);
        return Ok(await staffOperationsService.GetIncomingClaimsAsync(
            new ClaimMarketPageQuery(actorStaffUserId, marketCode, page, pageSize),
            cancellationToken));
    }

    [HttpGet("claims/unassigned")]
    [ProducesResponseType<PagedResult<StaffClaimListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StaffClaimListItem>>> GetUnassignedClaims(
        [FromQuery] string marketCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeStaffAsync(
            actorStaffUserId,
            ClaimAccessQueryFactory.GetAccessRole(User),
            marketCode,
            cancellationToken);
        return Ok(await staffOperationsService.GetUnassignedClaimsAsync(
            new ClaimMarketPageQuery(actorStaffUserId, marketCode, page, pageSize),
            cancellationToken));
    }

    [HttpGet("officers/{officerId:guid}/claims")]
    [ProducesResponseType<PagedResult<StaffClaimListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StaffClaimListItem>>> GetOfficerClaims(
        Guid officerId,
        [FromQuery] string marketCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeStaffAsync(
            actorStaffUserId,
            ClaimAccessQueryFactory.GetAccessRole(User),
            marketCode,
            cancellationToken);
        return Ok(await staffOperationsService.GetOfficerClaimsAsync(
            new OfficerClaimsPageQuery(actorStaffUserId, officerId, marketCode, page, pageSize),
            cancellationToken));
    }

    [HttpGet("officers/{officerId:guid}/workload")]
    [ProducesResponseType<OfficerWorkloadResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfficerWorkloadResult>> GetOfficerWorkload(
        Guid officerId,
        [FromQuery] string marketCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeStaffAsync(
            actorStaffUserId,
            ClaimAccessQueryFactory.GetAccessRole(User),
            marketCode,
            cancellationToken);
        return Ok(await staffOperationsService.GetOfficerWorkloadAsync(
            new OfficerWorkloadQuery(actorStaffUserId, officerId, marketCode),
            cancellationToken));
    }

    [HttpGet("claims/{claimReference}/assignments")]
    [ProducesResponseType<IReadOnlyList<ClaimAssignmentHistoryItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClaimAssignmentHistoryItem>>> GetAssignmentHistory(
        string claimReference,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);
        return Ok(await staffOperationsService.GetAssignmentHistoryAsync(
            new ClaimAssignmentHistoryQuery(actorStaffUserId, claimReference),
            cancellationToken));
    }

    [HttpPost("claims/{claimReference}/assignments")]
    [ProducesResponseType<ClaimAssignmentResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimAssignmentResult>> AssignClaim(
        string claimReference,
        [FromBody] AssignClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);
        var assignment = await staffOperationsService.AssignAsync(
            new AssignClaimCommand(actorStaffUserId, claimReference, request.OfficerId, request.Role),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetAssignmentHistory),
            new { version = HttpContext.Request.RouteValues["version"]?.ToString() ?? "1", claimReference },
            assignment);
    }

    [HttpPost("claims/{claimReference}/assignments/{assignmentId:guid}/reassign")]
    [ProducesResponseType<ClaimAssignmentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimAssignmentResult>> ReassignClaim(
        string claimReference,
        Guid assignmentId,
        [FromBody] ReassignClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var actorStaffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);
        return Ok(await staffOperationsService.ReassignAsync(
            new ReassignClaimCommand(
                actorStaffUserId,
                claimReference,
                assignmentId,
                request.NewOfficerId),
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
