using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimAuditEntry : Entity
{
    private ClaimAuditEntry()
    {
        Action = null!;
    }

    internal ClaimAuditEntry(
        Guid claimId,
        string action,
        DateTimeOffset occurredAtUtc,
        string? details,
        Guid? staffUserId,
        Guid? claimantId)
    {
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(
            !(staffUserId.HasValue && claimantId.HasValue),
            "An audit entry cannot have both a staff and claimant actor.");
        if (staffUserId.HasValue)
        {
            DomainRule.Require(staffUserId.Value != Guid.Empty, "Staff user identifier cannot be empty.");
        }

        if (claimantId.HasValue)
        {
            DomainRule.Require(claimantId.Value != Guid.Empty, "Claimant identifier cannot be empty.");
        }

        ClaimId = claimId;
        Action = DomainRule.RequiredText(action, nameof(action), 100);
        OccurredAtUtc = DomainRule.Utc(occurredAtUtc, nameof(occurredAtUtc));
        Details = string.IsNullOrWhiteSpace(details)
            ? null
            : DomainRule.RequiredText(details, nameof(details), 2000);
        StaffUserId = staffUserId;
        ClaimantId = claimantId;
    }

    public Guid ClaimId { get; private set; }

    public string Action { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Details { get; private set; }

    public Guid? StaffUserId { get; private set; }

    public Guid? ClaimantId { get; private set; }
}
