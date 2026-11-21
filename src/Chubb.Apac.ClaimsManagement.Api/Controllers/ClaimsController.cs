using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Api.Infrastructure;
using Chubb.Apac.ClaimsManagement.Api.Models;
using Chubb.Apac.ClaimsManagement.Application.Security;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/claims")]
public sealed class ClaimsController(
    IClaimSubmissionService claimSubmissionService,
    IClaimAccessAuthorizationService claimAccessAuthorizationService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Claimant")]
    [ProducesResponseType<ClaimSubmissionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(
        [FromBody] ClaimSubmissionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var tokenEmail = User.FindFirst("email")?.Value;
        if (!ClaimAccessQueryFactory.HasVerifiedEmail(User) ||
            string.IsNullOrWhiteSpace(tokenEmail) ||
            !string.Equals(
                request.Claimant?.Email?.Trim(),
                tokenEmail.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var command = new SubmitClaimCommand(
            idempotencyKey ?? string.Empty,
            request.MarketCode ?? string.Empty,
            request.PolicyNumber ?? string.Empty,
            request.ClaimType,
            request.IncidentAtUtc,
            request.IncidentDescription ?? string.Empty,
            request.Claimant?.Name ?? string.Empty,
            request.Claimant?.Email,
            request.Claimant?.Phone,
            request.EstimatedLossAmount,
            request.CurrencyCode ?? string.Empty);

        var result = await claimSubmissionService.SubmitAsync(command, cancellationToken);
        var trackingUrl = Url.Link(
            nameof(GetTracking),
            new
            {
                version = HttpContext.Request.RouteValues["version"]?.ToString() ?? "1",
                claimReference = result.ClaimReference
            });

        if (trackingUrl is null)
        {
            throw new InvalidOperationException("Could not generate the claim tracking URL.");
        }

        var response = new ClaimSubmissionResponse(
            result.ClaimReference,
            result.Status,
            result.SubmittedAtUtc,
            trackingUrl);
        return Created(trackingUrl, response);
    }

    [HttpGet("{claimReference}", Name = nameof(GetTracking))]
    [Authorize(Roles = "Claimant,ClaimsOfficer,Manager")]
    [ProducesResponseType<ClaimTrackingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimTrackingResponse>> GetTracking(
        string claimReference,
        CancellationToken cancellationToken)
    {
        await claimAccessAuthorizationService.AuthorizeAsync(
            ClaimAccessQueryFactory.Create(claimReference, User),
            cancellationToken);

        var result = await claimSubmissionService.GetTrackingAsync(claimReference, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(new ClaimTrackingResponse(
            result.ClaimReference,
            result.Status,
            result.SubmittedAtUtc));
    }
}
