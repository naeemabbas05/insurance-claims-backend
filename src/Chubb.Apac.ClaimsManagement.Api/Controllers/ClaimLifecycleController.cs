using System.Security.Claims;
using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Api.Infrastructure;
using Chubb.Apac.ClaimsManagement.Api.Models;
using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/claims/{claimReference}")]
public sealed class ClaimLifecycleController(
    IClaimLifecycleService lifecycleService,
    IClaimAccessAuthorizationService claimAccessAuthorizationService) : ControllerBase
{
    [HttpGet("history")]
    [Authorize(Roles = "Claimant,ClaimsOfficer,Manager")]
    [ProducesResponseType<IReadOnlyList<ClaimStatusHistoryItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClaimStatusHistoryItem>>> GetStatusHistory(
        string claimReference,
        CancellationToken cancellationToken)
    {
        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);
        return Ok(await lifecycleService.GetStatusHistoryAsync(claimReference, cancellationToken));
    }

    [HttpGet("audit")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<IReadOnlyList<ClaimAuditHistoryItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClaimAuditHistoryItem>>> GetAuditHistory(
        string claimReference,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);

        return Ok(await lifecycleService.GetAuditHistoryAsync(
            claimReference,
            staffUserId,
            cancellationToken));
    }

    [HttpPost("assessments")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> RecordAssessment(
        string claimReference,
        [FromBody] RecordClaimAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        var result = await lifecycleService.RecordAssessmentAsync(
            new RecordClaimAssessmentCommand(
                claimReference,
                staffUserId,
                request.AssessedDamageAmount,
                request.RecommendedSettlementAmount,
                request.CurrencyCode ?? string.Empty,
                request.Findings ?? string.Empty),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("approval")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> Approve(
        string claimReference,
        [FromBody] ApproveClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.ApproveAsync(
            new ApproveClaimCommand(
                claimReference,
                staffUserId,
                request.ApprovedAmount,
                request.CurrencyCode ?? string.Empty,
                request.Reason ?? string.Empty),
            cancellationToken));
    }

    [HttpPost("rejection")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> Reject(
        string claimReference,
        [FromBody] RejectClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.RejectAsync(
            new RejectClaimCommand(claimReference, staffUserId, request.Reason ?? string.Empty),
            cancellationToken));
    }

    [HttpPost("information-requests")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> RequestInformation(
        string claimReference,
        [FromBody] RequestClaimInformationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.RequestInformationAsync(
            new RequestClaimInformationCommand(
                claimReference,
                staffUserId,
                request.ClaimantId,
                request.Question ?? string.Empty,
                request.DueAtUtc),
            cancellationToken));
    }

    [HttpPost("information-requests/{requestId:guid}/responses")]
    [Authorize(Roles = "Claimant")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> RespondToInformation(
        string claimReference,
        Guid requestId,
        [FromBody] RespondToClaimInformationRequest request,
        CancellationToken cancellationToken)
    {
        var access = await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.CreateClaimantQuery(claimReference, User),
            cancellationToken);
        var claimantId = access.ClaimantId
            ?? throw new ClaimAccessDeniedException();

        return Ok(await lifecycleService.RespondToInformationAsync(
            new RespondToClaimInformationCommand(
                claimReference,
                claimantId,
                requestId,
                request.Response ?? string.Empty),
            cancellationToken));
    }

    [HttpDelete("information-requests/{requestId:guid}")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> CancelInformationRequest(
        string claimReference,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.CancelInformationRequestAsync(
            new CancelClaimInformationCommand(claimReference, staffUserId, requestId),
            cancellationToken));
    }

    [HttpPost("settlements")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> ProposeSettlement(
        string claimReference,
        [FromBody] ProposeClaimSettlementRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.ProposeSettlementAsync(
            new ProposeClaimSettlementCommand(
                claimReference,
                staffUserId,
                request.PayeeClaimantId,
                request.Amount,
                request.CurrencyCode ?? string.Empty,
                request.IsFinal),
            cancellationToken));
    }

    [HttpPost("settlements/{settlementId:guid}/approval")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> ApproveSettlement(
        string claimReference,
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.ApproveSettlementAsync(
            new ApproveClaimSettlementCommand(claimReference, staffUserId, settlementId),
            cancellationToken));
    }

    [HttpPost("settlements/{settlementId:guid}/payment")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> PaySettlement(
        string claimReference,
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.PaySettlementAsync(
            new PayClaimSettlementCommand(claimReference, staffUserId, settlementId),
            cancellationToken));
    }

    [HttpDelete("settlements/{settlementId:guid}")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> CancelSettlement(
        string claimReference,
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.CancelSettlementAsync(
            new CancelClaimSettlementCommand(claimReference, staffUserId, settlementId),
            cancellationToken));
    }

    [HttpPost("workflow-status")]
    [Authorize(Roles = "ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimLifecycleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimLifecycleResult>> ChangeWorkflowStatus(
        string claimReference,
        [FromBody] ChangeClaimWorkflowStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffUserId(out var staffUserId))
        {
            return Unauthorized();
        }

        await AuthorizeStaffClaimAsync(claimReference, cancellationToken);
        return Ok(await lifecycleService.ChangeWorkflowStatusAsync(
            new ChangeClaimWorkflowStatusCommand(
                claimReference,
                staffUserId,
                request.NewStatus,
                request.Reason ?? string.Empty),
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

    private Task<ClaimAccessResult> AuthorizeStaffClaimAsync(
        string claimReference,
        CancellationToken cancellationToken) =>
        claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);
}
