using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimSettlement : Entity
{
    private ClaimSettlement()
    {
        Amount = null!;
        Payee = null!;
    }

    internal ClaimSettlement(
        Guid claimId,
        Claimant payee,
        Money amount,
        bool isFinal,
        StaffUser proposedBy,
        DateTimeOffset proposedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(payee);
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(proposedBy);
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(amount.Amount > 0, "Settlement amount must be greater than zero.");
        DomainRule.Require(proposedBy.IsActive, "Inactive staff cannot propose a settlement.");

        ClaimId = claimId;
        PayeeClaimantId = payee.Id;
        Payee = payee;
        Amount = amount;
        IsFinal = isFinal;
        ProposedByStaffUserId = proposedBy.Id;
        ProposedAtUtc = DomainRule.Utc(proposedAtUtc, nameof(proposedAtUtc));
        Status = SettlementStatus.Proposed;
    }

    public Guid ClaimId { get; private set; }

    public Guid PayeeClaimantId { get; private set; }

    public Claimant Payee { get; private set; }

    public Money Amount { get; private set; }

    public bool IsFinal { get; private set; }

    public SettlementStatus Status { get; private set; }

    public Guid ProposedByStaffUserId { get; private set; }

    public DateTimeOffset ProposedAtUtc { get; private set; }

    public Guid? ApprovedByStaffUserId { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public Guid? PaidByStaffUserId { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    internal void Approve(StaffUser approver, DateTimeOffset approvedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(approver);
        DomainRule.Require(Status == SettlementStatus.Proposed, "Only proposed settlements can be approved.");
        DomainRule.Require(approver.IsActive, "Inactive staff cannot approve settlements.");
        DomainRule.Utc(approvedAtUtc, nameof(approvedAtUtc));
        DomainRule.Require(approvedAtUtc >= ProposedAtUtc, "Approval cannot precede the proposal.");

        ApprovedByStaffUserId = approver.Id;
        ApprovedAtUtc = approvedAtUtc;
        Status = SettlementStatus.Approved;
    }

    internal void MarkPaid(StaffUser financeOfficer, DateTimeOffset paidAtUtc)
    {
        ArgumentNullException.ThrowIfNull(financeOfficer);
        DomainRule.Require(Status == SettlementStatus.Approved, "Only approved settlements can be paid.");
        DomainRule.Require(financeOfficer.IsActive, "Inactive staff cannot mark settlements as paid.");
        DomainRule.Utc(paidAtUtc, nameof(paidAtUtc));
        DomainRule.Require(paidAtUtc >= ApprovedAtUtc, "Payment cannot precede settlement approval.");

        PaidByStaffUserId = financeOfficer.Id;
        PaidAtUtc = paidAtUtc;
        Status = SettlementStatus.Paid;
    }

    internal void Cancel(DateTimeOffset cancelledAtUtc)
    {
        DomainRule.Require(
            Status is SettlementStatus.Proposed or SettlementStatus.Approved,
            "Only proposed or approved settlements can be cancelled.");
        DomainRule.Utc(cancelledAtUtc, nameof(cancelledAtUtc));
        DomainRule.Require(cancelledAtUtc >= ProposedAtUtc, "Cancellation cannot precede the proposal.");
        CancelledAtUtc = cancelledAtUtc;
        Status = SettlementStatus.Cancelled;
    }
}
