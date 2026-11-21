using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimStatusHistory : Entity
{
    private ClaimStatusHistory()
    {
        Reason = null!;
    }

    internal ClaimStatusHistory(
        Guid claimId,
        ClaimStatus? previousStatus,
        ClaimStatus newStatus,
        DateTimeOffset changedAtUtc,
        Guid? changedByStaffUserId,
        string reason)
    {
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(Enum.IsDefined(newStatus), "Claim status is invalid.");
        if (previousStatus.HasValue)
        {
            DomainRule.Require(Enum.IsDefined(previousStatus.Value), "Previous claim status is invalid.");
        }

        if (changedByStaffUserId.HasValue)
        {
            DomainRule.Require(changedByStaffUserId.Value != Guid.Empty, "Staff user identifier cannot be empty.");
        }

        ClaimId = claimId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAtUtc = DomainRule.Utc(changedAtUtc, nameof(changedAtUtc));
        ChangedByStaffUserId = changedByStaffUserId;
        Reason = DomainRule.RequiredText(reason, nameof(reason), 1000);
    }

    public Guid ClaimId { get; private set; }

    public ClaimStatus? PreviousStatus { get; private set; }

    public ClaimStatus NewStatus { get; private set; }

    public DateTimeOffset ChangedAtUtc { get; private set; }

    public Guid? ChangedByStaffUserId { get; private set; }

    public string Reason { get; private set; }
}
