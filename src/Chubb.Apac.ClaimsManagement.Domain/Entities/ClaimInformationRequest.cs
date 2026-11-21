using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimInformationRequest : Entity
{
    private ClaimInformationRequest()
    {
        Question = null!;
        RequestedFrom = null!;
        RequestedBy = null!;
    }

    internal ClaimInformationRequest(
        Guid claimId,
        Claimant requestedFrom,
        StaffUser requestedBy,
        string question,
        DateTimeOffset requestedAtUtc,
        DateTimeOffset dueAtUtc)
    {
        ArgumentNullException.ThrowIfNull(requestedFrom);
        ArgumentNullException.ThrowIfNull(requestedBy);
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(requestedFrom.ClaimId == claimId, "Requested claimant does not belong to this claim.");
        DomainRule.Require(requestedBy.IsActive, "Inactive staff cannot request information.");

        ClaimId = claimId;
        RequestedFromClaimantId = requestedFrom.Id;
        RequestedFrom = requestedFrom;
        RequestedByStaffUserId = requestedBy.Id;
        RequestedBy = requestedBy;
        Question = DomainRule.RequiredText(question, nameof(question), 2000);
        RequestedAtUtc = DomainRule.Utc(requestedAtUtc, nameof(requestedAtUtc));
        DueAtUtc = DomainRule.Utc(dueAtUtc, nameof(dueAtUtc));
        DomainRule.Require(DueAtUtc > RequestedAtUtc, "Information request due time must be after the request time.");
        Status = InformationRequestStatus.Open;
    }

    public Guid ClaimId { get; private set; }

    public Guid RequestedFromClaimantId { get; private set; }

    public Claimant RequestedFrom { get; private set; }

    public Guid RequestedByStaffUserId { get; private set; }

    public StaffUser RequestedBy { get; private set; }

    public string Question { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset DueAtUtc { get; private set; }

    public InformationRequestStatus Status { get; private set; }

    public string? Response { get; private set; }

    public DateTimeOffset? RespondedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public bool IsOverdueAt(DateTimeOffset instantUtc)
    {
        DomainRule.Utc(instantUtc, nameof(instantUtc));
        return Status == InformationRequestStatus.Open && instantUtc > DueAtUtc;
    }

    internal void Respond(Claimant claimant, string response, DateTimeOffset respondedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(claimant);
        DomainRule.Require(Status == InformationRequestStatus.Open, "Only open information requests can be answered.");
        DomainRule.Require(claimant.Id == RequestedFromClaimantId, "Only the requested claimant can respond.");
        DomainRule.Utc(respondedAtUtc, nameof(respondedAtUtc));
        DomainRule.Require(respondedAtUtc >= RequestedAtUtc, "Response cannot precede the request.");

        Response = DomainRule.RequiredText(response, nameof(response), 4000);
        RespondedAtUtc = respondedAtUtc;
        Status = InformationRequestStatus.Responded;
    }

    internal void Cancel(StaffUser staffUser, DateTimeOffset cancelledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(staffUser);
        DomainRule.Require(Status == InformationRequestStatus.Open, "Only open information requests can be cancelled.");
        DomainRule.Require(staffUser.IsActive, "Inactive staff cannot cancel information requests.");
        DomainRule.Utc(cancelledAtUtc, nameof(cancelledAtUtc));
        DomainRule.Require(cancelledAtUtc >= RequestedAtUtc, "Cancellation cannot precede the request.");
        CancelledAtUtc = cancelledAtUtc;
        Status = InformationRequestStatus.Cancelled;
    }
}
