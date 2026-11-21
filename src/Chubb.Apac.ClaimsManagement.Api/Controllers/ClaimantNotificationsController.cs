using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Asp.Versioning;
using Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Apac.ClaimsManagement.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/claimant/notifications")]
[Authorize(Roles = "Claimant")]
public sealed class ClaimantNotificationsController(
    IClaimantNotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ClaimantNotificationPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClaimantNotificationPage>> GetNotifications(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentity(out var claimantId, out var email, out var emailVerified))
        {
            return Forbid();
        }

        return Ok(await notificationService.GetNotificationsAsync(
            new ClaimantNotificationQuery(claimantId, email, emailVerified, page, pageSize, unreadOnly),
            cancellationToken));
    }

    [HttpPut("{notificationId:guid}/read")]
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType<ClaimantNotificationItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimantNotificationItem>> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var claimantId, out var email, out var emailVerified))
        {
            return Forbid();
        }

        var notification = await notificationService.MarkReadAsync(
            notificationId,
            claimantId,
            email,
            emailVerified,
            cancellationToken);
        return notification is null ? NotFound() : Ok(notification);
    }

    private bool TryGetIdentity(out Guid? claimantId, out string? email, out bool emailVerified)
    {
        claimantId = Guid.TryParse(User.FindFirstValue("claimant_id"), out var parsedId) &&
                     parsedId != Guid.Empty
            ? parsedId
            : null;
        emailVerified = string.Equals(
            User.FindFirstValue("email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        email = emailVerified ? User.FindFirstValue("email") : null;
        return claimantId.HasValue || !string.IsNullOrWhiteSpace(email);
    }
}
