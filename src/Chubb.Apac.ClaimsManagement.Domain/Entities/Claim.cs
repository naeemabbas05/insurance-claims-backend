using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class Claim : Entity
{
    private static readonly IReadOnlyDictionary<ClaimStatus, HashSet<ClaimStatus>> AllowedTransitions =
        new Dictionary<ClaimStatus, HashSet<ClaimStatus>>
        {
            [ClaimStatus.Submitted] = [ClaimStatus.UnderReview, ClaimStatus.Closed],
            [ClaimStatus.UnderReview] =
            [
                ClaimStatus.AwaitingInformation,
                ClaimStatus.AssessmentInProgress,
                ClaimStatus.Approved,
                ClaimStatus.Declined,
                ClaimStatus.Closed
            ],
            [ClaimStatus.AwaitingInformation] = [ClaimStatus.UnderReview, ClaimStatus.Closed],
            [ClaimStatus.AssessmentInProgress] =
            [
                ClaimStatus.UnderReview,
                ClaimStatus.AwaitingInformation,
                ClaimStatus.Approved,
                ClaimStatus.Declined,
                ClaimStatus.Closed
            ],
            [ClaimStatus.Approved] = [ClaimStatus.Settled, ClaimStatus.Closed],
            [ClaimStatus.Declined] = [ClaimStatus.UnderReview, ClaimStatus.Closed],
            [ClaimStatus.Settled] = [ClaimStatus.Closed],
            [ClaimStatus.Closed] = []
        };

    private readonly List<Claimant> _claimants = [];
    private readonly List<ClaimDocument> _documents = [];
    private readonly List<ClaimAssignment> _assignments = [];
    private readonly List<ClaimAssessment> _assessments = [];
    private readonly List<ClaimStatusHistory> _statusHistory = [];
    private readonly List<ClaimInformationRequest> _informationRequests = [];
    private readonly List<ClaimSettlement> _settlements = [];

    private Claim()
    {
        Number = null!;
        Policy = null!;
        LossDescription = null!;
    }

    private Claim(
        ClaimNumber number,
        Policy policy,
        ClaimType type,
        DateTimeOffset incidentAtUtc,
        DateTimeOffset reportedAtUtc,
        string lossDescription,
        ClaimantDetails primaryClaimant)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(primaryClaimant);
        DomainRule.Require(Enum.IsDefined(type), "Claim type is invalid.");
        DomainRule.Require(
            (type == ClaimType.Motor && policy.Type == PolicyType.Motor) ||
            (type == ClaimType.Property && policy.Type == PolicyType.Property),
            "Claim type must match the policy type.");

        IncidentAtUtc = DomainRule.Utc(incidentAtUtc, nameof(incidentAtUtc));
        ReportedAtUtc = DomainRule.Utc(reportedAtUtc, nameof(reportedAtUtc));
        DomainRule.Require(ReportedAtUtc >= IncidentAtUtc, "A claim cannot be reported before the incident.");
        DomainRule.Require(policy.IsInForceAt(IncidentAtUtc), "Policy must be active and in force at the time of loss.");

        Number = number;
        PolicyId = policy.Id;
        Policy = policy;
        Type = type;
        LossDescription = DomainRule.RequiredText(lossDescription, nameof(lossDescription), 4000);
        CreatedAtUtc = ReportedAtUtc;
        Status = ClaimStatus.Submitted;
        _claimants.Add(new Claimant(Id, primaryClaimant, ReportedAtUtc));
        _statusHistory.Add(new ClaimStatusHistory(
            Id,
            previousStatus: null,
            ClaimStatus.Submitted,
            ReportedAtUtc,
            changedByStaffUserId: null,
            "Claim submitted."));
    }

    public ClaimNumber Number { get; private set; }

    public Guid PolicyId { get; private set; }

    public Policy Policy { get; private set; }

    public ClaimType Type { get; private set; }

    public DateTimeOffset IncidentAtUtc { get; private set; }

    public DateTimeOffset ReportedAtUtc { get; private set; }

    public string LossDescription { get; private set; }

    public ClaimStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Claimant> Claimants => _claimants.AsReadOnly();

    public IReadOnlyCollection<ClaimDocument> Documents => _documents.AsReadOnly();

    public IReadOnlyCollection<ClaimAssignment> Assignments => _assignments.AsReadOnly();

    public IReadOnlyCollection<ClaimAssessment> Assessments => _assessments.AsReadOnly();

    public IReadOnlyCollection<ClaimStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public IReadOnlyCollection<ClaimInformationRequest> InformationRequests => _informationRequests.AsReadOnly();

    public IReadOnlyCollection<ClaimSettlement> Settlements => _settlements.AsReadOnly();

    public ClaimAssessment? LatestAssessment => _assessments
        .OrderByDescending(assessment => assessment.AssessedAtUtc)
        .FirstOrDefault();

    public static Claim Create(
        ClaimNumber number,
        Policy policy,
        ClaimType type,
        DateTimeOffset incidentAtUtc,
        DateTimeOffset reportedAtUtc,
        string lossDescription,
        ClaimantDetails primaryClaimant)
    {
        var claim = new Claim(
            number,
            policy,
            type,
            incidentAtUtc,
            reportedAtUtc,
            lossDescription,
            primaryClaimant);
        policy.AttachClaim(claim);
        return claim;
    }

    public Claimant AddClaimant(ClaimantDetails details, DateTimeOffset addedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(details);
        EnsureNotClosed();
        DomainRule.Utc(addedAtUtc, nameof(addedAtUtc));
        DomainRule.Require(addedAtUtc >= ReportedAtUtc, "Claimant cannot be added before the claim was reported.");

        var claimant = new Claimant(Id, details, addedAtUtc);
        _claimants.Add(claimant);
        return claimant;
    }

    public ClaimAssignment Assign(
        StaffUser staffUser,
        AssignmentRole role,
        DateTimeOffset assignedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(staffUser);
        EnsureNotClosed();
        EnsureStaffCanPerform(staffUser, ToStaffRole(role));
        DomainRule.Require(Enum.IsDefined(role), "Assignment role is invalid.");
        DomainRule.Utc(assignedAtUtc, nameof(assignedAtUtc));
        DomainRule.Require(assignedAtUtc >= ReportedAtUtc, "Assignment cannot precede claim submission.");
        DomainRule.Require(
            !_assignments.Any(assignment =>
                assignment.IsActive && assignment.StaffUserId == staffUser.Id && assignment.Role == role),
            "Staff user already has an active assignment for this claim and role.");

        var assignment = new ClaimAssignment(Id, staffUser, role, assignedAtUtc);
        _assignments.Add(assignment);
        return assignment;
    }

    public void Unassign(Guid assignmentId, StaffUser unassignedBy, DateTimeOffset unassignedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(unassignedBy);
        EnsureStaffCanPerform(unassignedBy, StaffRole.ClaimsHandler);
        FindEntity(_assignments, assignmentId, "Claim assignment").Unassign(unassignedAtUtc);
    }

    public ClaimDocument AddDocument(
        ClaimDocumentType type,
        ClaimDocumentMetadata metadata,
        DateTimeOffset uploadedAtUtc,
        Claimant? uploadedByClaimant = null,
        StaffUser? uploadedByStaff = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        EnsureNotClosed();
        DomainRule.Utc(uploadedAtUtc, nameof(uploadedAtUtc));
        DomainRule.Require(uploadedAtUtc >= ReportedAtUtc, "Document upload cannot precede claim submission.");
        DomainRule.Require(
            uploadedByClaimant is not null ^ uploadedByStaff is not null,
            "Exactly one claimant or staff uploader must be specified.");

        if (uploadedByClaimant is not null)
        {
            EnsureClaimantBelongs(uploadedByClaimant);
        }

        if (uploadedByStaff is not null)
        {
            EnsureStaffCanWorkInMarket(uploadedByStaff);
        }

        var document = new ClaimDocument(
            Id,
            type,
            metadata,
            uploadedByClaimant?.Id,
            uploadedByStaff?.Id,
            uploadedAtUtc);
        _documents.Add(document);
        return document;
    }

    public ClaimAssessment RecordAssessment(
        StaffUser assessor,
        Money assessedDamage,
        Money recommendedSettlement,
        string findings,
        DateTimeOffset assessedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(assessedDamage);
        ArgumentNullException.ThrowIfNull(recommendedSettlement);
        EnsureStaffCanPerform(assessor, StaffRole.Assessor);
        DomainRule.Require(
            Status is ClaimStatus.UnderReview or ClaimStatus.AssessmentInProgress,
            "Assessment can only be recorded while the claim is under review.");
        DomainRule.Utc(assessedAtUtc, nameof(assessedAtUtc));
        DomainRule.Require(assessedAtUtc >= ReportedAtUtc, "Assessment cannot precede claim submission.");
        if (Status == ClaimStatus.UnderReview)
        {
            EnsureNotBeforeLastStatusChange(assessedAtUtc);
        }
        DomainRule.Require(
            assessedDamage.Currency == Policy.CoverageLimit.Currency &&
            recommendedSettlement.Currency == Policy.CoverageLimit.Currency,
            "Assessment amounts must use the policy currency.");
        DomainRule.Require(
            recommendedSettlement.Amount <= Policy.CoverageLimit.Amount,
            "Recommended settlement cannot exceed the policy coverage limit.");

        var assessment = new ClaimAssessment(
            Id,
            assessor,
            assessedDamage,
            recommendedSettlement,
            findings,
            assessedAtUtc);
        _assessments.Add(assessment);

        if (Status == ClaimStatus.UnderReview)
        {
            TransitionTo(
                ClaimStatus.AssessmentInProgress,
                assessor,
                "Assessment recorded.",
                assessedAtUtc,
                requireStaffActor: true);
        }

        return assessment;
    }

    public ClaimInformationRequest RequestInformation(
        StaffUser requestedBy,
        Claimant requestedFrom,
        string question,
        DateTimeOffset requestedAtUtc,
        DateTimeOffset dueAtUtc)
    {
        ArgumentNullException.ThrowIfNull(requestedBy);
        ArgumentNullException.ThrowIfNull(requestedFrom);
        EnsureStaffCanPerformAny(requestedBy, StaffRole.ClaimsHandler, StaffRole.Adjuster, StaffRole.Assessor);
        EnsureClaimantBelongs(requestedFrom);
        DomainRule.Require(
            Status is ClaimStatus.UnderReview or ClaimStatus.AssessmentInProgress or ClaimStatus.AwaitingInformation,
            "Information can only be requested while a claim is being reviewed.");
        DomainRule.Utc(requestedAtUtc, nameof(requestedAtUtc));
        DomainRule.Require(requestedAtUtc >= ReportedAtUtc, "Information request cannot precede claim submission.");
        EnsureNotBeforeLastStatusChange(requestedAtUtc);

        var request = new ClaimInformationRequest(
            Id,
            requestedFrom,
            requestedBy,
            question,
            requestedAtUtc,
            dueAtUtc);
        _informationRequests.Add(request);

        if (Status != ClaimStatus.AwaitingInformation)
        {
            TransitionTo(
                ClaimStatus.AwaitingInformation,
                requestedBy,
                "Additional information was requested.",
                requestedAtUtc,
                requireStaffActor: true);
        }

        return request;
    }

    public void RespondToInformationRequest(
        Guid requestId,
        Claimant claimant,
        string response,
        DateTimeOffset respondedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(claimant);
        EnsureClaimantBelongs(claimant);
        DomainRule.Utc(respondedAtUtc, nameof(respondedAtUtc));
        if (Status == ClaimStatus.AwaitingInformation &&
            _informationRequests.Count(request => request.Status == InformationRequestStatus.Open) == 1)
        {
            EnsureNotBeforeLastStatusChange(respondedAtUtc);
        }

        FindEntity(_informationRequests, requestId, "Information request")
            .Respond(claimant, response, respondedAtUtc);

        if (Status == ClaimStatus.AwaitingInformation &&
            _informationRequests.All(request => request.Status != InformationRequestStatus.Open))
        {
            TransitionTo(
                ClaimStatus.UnderReview,
                actor: null,
                "All requested information has been received.",
                respondedAtUtc,
                requireStaffActor: false);
        }
    }

    public void CancelInformationRequest(
        Guid requestId,
        StaffUser cancelledBy,
        DateTimeOffset cancelledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(cancelledBy);
        EnsureStaffCanPerformAny(cancelledBy, StaffRole.ClaimsHandler, StaffRole.Adjuster);
        DomainRule.Utc(cancelledAtUtc, nameof(cancelledAtUtc));
        if (Status == ClaimStatus.AwaitingInformation &&
            _informationRequests.Count(request => request.Status == InformationRequestStatus.Open) == 1)
        {
            EnsureNotBeforeLastStatusChange(cancelledAtUtc);
        }

        FindEntity(_informationRequests, requestId, "Information request")
            .Cancel(cancelledBy, cancelledAtUtc);

        if (Status == ClaimStatus.AwaitingInformation &&
            _informationRequests.All(request => request.Status != InformationRequestStatus.Open))
        {
            TransitionTo(
                ClaimStatus.UnderReview,
                cancelledBy,
                "All information requests were closed.",
                cancelledAtUtc,
                requireStaffActor: true);
        }
    }

    public ClaimSettlement ProposeSettlement(
        Claimant payee,
        Money amount,
        bool isFinal,
        StaffUser proposedBy,
        DateTimeOffset proposedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(payee);
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(proposedBy);
        EnsureClaimantBelongs(payee);
        EnsureStaffCanPerformAny(proposedBy, StaffRole.ClaimsHandler, StaffRole.Approver);
        DomainRule.Require(Status == ClaimStatus.Approved, "Settlement can only be proposed for an approved claim.");
        DomainRule.Require(
            !_informationRequests.Any(request => request.Status == InformationRequestStatus.Open),
            "Settlement cannot be proposed while information is outstanding.");
        DomainRule.Utc(proposedAtUtc, nameof(proposedAtUtc));
        DomainRule.Require(proposedAtUtc >= ReportedAtUtc, "Settlement cannot precede claim submission.");
        DomainRule.Require(amount.Currency == Policy.CoverageLimit.Currency, "Settlement must use the policy currency.");
        DomainRule.Require(amount.Amount > 0, "Settlement amount must be greater than zero.");
        EnsureNotBeforeLastStatusChange(proposedAtUtc);

        var assessment = LatestAssessment
            ?? throw new DomainRuleViolationException("A completed assessment is required before settlement.");
        DomainRule.Require(
            !_settlements.Any(settlement =>
                settlement.IsFinal && settlement.Status != SettlementStatus.Cancelled),
            "A final settlement has already been proposed.");

        var activeTotal = _settlements
            .Where(settlement => settlement.Status != SettlementStatus.Cancelled)
            .Aggregate(Money.Zero(amount.Currency), (total, settlement) => total.Add(settlement.Amount));
        DomainRule.Require(
            activeTotal.Add(amount).Amount <= assessment.RecommendedSettlement.Amount,
            "Total settlements cannot exceed the latest recommended settlement.");

        if (isFinal)
        {
            DomainRule.Require(
                _settlements.Where(settlement => settlement.Status != SettlementStatus.Cancelled)
                    .All(settlement => settlement.Status == SettlementStatus.Paid),
                "A final settlement requires all earlier settlements to be paid or cancelled.");
        }

        var settlement = new ClaimSettlement(Id, payee, amount, isFinal, proposedBy, proposedAtUtc);
        _settlements.Add(settlement);
        return settlement;
    }

    public void ApproveSettlement(
        Guid settlementId,
        StaffUser approver,
        DateTimeOffset approvedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(approver);
        EnsureStaffCanPerform(approver, StaffRole.Approver);
        FindEntity(_settlements, settlementId, "Settlement").Approve(approver, approvedAtUtc);
    }

    public void MarkSettlementPaid(
        Guid settlementId,
        StaffUser financeOfficer,
        DateTimeOffset paidAtUtc)
    {
        ArgumentNullException.ThrowIfNull(financeOfficer);
        EnsureStaffCanPerform(financeOfficer, StaffRole.FinanceOfficer);
        var settlement = FindEntity(_settlements, settlementId, "Settlement");
        if (settlement.IsFinal &&
            _settlements.Where(item => item.Id != settlement.Id && item.Status != SettlementStatus.Cancelled)
                .All(item => item.Status == SettlementStatus.Paid))
        {
            EnsureNotBeforeLastStatusChange(paidAtUtc);
        }

        settlement.MarkPaid(financeOfficer, paidAtUtc);

        if (settlement.IsFinal &&
            _settlements.Where(item => item.Status != SettlementStatus.Cancelled)
                .All(item => item.Status == SettlementStatus.Paid))
        {
            TransitionTo(
                ClaimStatus.Settled,
                financeOfficer,
                "Final settlement was paid.",
                paidAtUtc,
                requireStaffActor: true,
                allowSettled: true);
        }
    }

    public void CancelSettlement(
        Guid settlementId,
        StaffUser cancelledBy,
        DateTimeOffset cancelledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(cancelledBy);
        EnsureStaffCanPerform(cancelledBy, StaffRole.Approver);
        FindEntity(_settlements, settlementId, "Settlement").Cancel(cancelledAtUtc);
    }

    public void ChangeStatus(
        ClaimStatus newStatus,
        StaffUser changedBy,
        string reason,
        DateTimeOffset changedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(changedBy);
        DomainRule.Require(Enum.IsDefined(newStatus), "Claim status is invalid.");
        EnsureStaffCanWorkInMarket(changedBy);

        var requiredRole = newStatus switch
        {
            ClaimStatus.Approved or ClaimStatus.Declined => StaffRole.Approver,
            ClaimStatus.UnderReview or ClaimStatus.AwaitingInformation or
                ClaimStatus.AssessmentInProgress or ClaimStatus.Closed => StaffRole.ClaimsHandler,
            ClaimStatus.Settled => throw new DomainRuleViolationException(
                "A claim is settled only when its final settlement is paid."),
            _ => throw new DomainRuleViolationException($"Claim cannot be transitioned to '{newStatus}'.")
        };
        DomainRule.Require(changedBy.CanPerform(requiredRole), $"Staff user cannot transition a claim to '{newStatus}'.");
        TransitionTo(newStatus, changedBy, reason, changedAtUtc, requireStaffActor: true);
    }

    private void TransitionTo(
        ClaimStatus newStatus,
        StaffUser? actor,
        string reason,
        DateTimeOffset changedAtUtc,
        bool requireStaffActor,
        bool allowSettled = false)
    {
        DomainRule.Utc(changedAtUtc, nameof(changedAtUtc));
        DomainRule.Require(
            _statusHistory.Count == 0 || changedAtUtc >= _statusHistory[^1].ChangedAtUtc,
            "Claim status changes cannot precede prior status history.");
        DomainRule.Require(Status != newStatus, $"Claim is already in status '{newStatus}'.");
        DomainRule.Require(
            AllowedTransitions.TryGetValue(Status, out var destinations) && destinations.Contains(newStatus),
            $"Claim cannot transition from '{Status}' to '{newStatus}'.");
        DomainRule.Require(
            newStatus != ClaimStatus.Settled || allowSettled,
            "A claim is settled only when its final settlement is paid.");

        if (requireStaffActor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            EnsureStaffCanWorkInMarket(actor);
        }

        if (newStatus == ClaimStatus.Approved)
        {
            DomainRule.Require(LatestAssessment is not null, "A completed assessment is required for approval.");
            DomainRule.Require(
                LatestAssessment!.RecommendedSettlement.Amount > 0,
                "A claim with no recommended settlement cannot be approved.");
            EnsureNoOpenInformationRequests();
        }

        if (newStatus is ClaimStatus.Declined or ClaimStatus.Closed)
        {
            EnsureNoOpenInformationRequests();
        }

        if (newStatus == ClaimStatus.AwaitingInformation)
        {
            DomainRule.Require(
                _informationRequests.Any(request => request.Status == InformationRequestStatus.Open),
                "A claim can await information only while an information request is open.");
        }

        _statusHistory.Add(new ClaimStatusHistory(
            Id,
            Status,
            newStatus,
            changedAtUtc,
            actor?.Id,
            reason));
        Status = newStatus;
    }

    private void EnsureNoOpenInformationRequests() =>
        DomainRule.Require(
            _informationRequests.All(request => request.Status != InformationRequestStatus.Open),
            "Claim cannot be approved, declined, or closed while information is outstanding.");

    private void EnsureNotClosed() =>
        DomainRule.Require(Status != ClaimStatus.Closed, "A closed claim cannot be modified.");

    private void EnsureNotBeforeLastStatusChange(DateTimeOffset timestampUtc) =>
        DomainRule.Require(
            _statusHistory.Count == 0 || timestampUtc >= _statusHistory[^1].ChangedAtUtc,
            "Claim activity cannot precede the latest status change.");

    private void EnsureClaimantBelongs(Claimant claimant) =>
        DomainRule.Require(
            claimant.ClaimId == Id && _claimants.Any(existing => existing.Id == claimant.Id),
            "Claimant does not belong to this claim.");

    private void EnsureStaffCanWorkInMarket(StaffUser staffUser)
    {
        DomainRule.Require(staffUser.IsActive, "Inactive staff cannot work on a claim.");
        DomainRule.Require(
            staffUser.CanWorkIn(Policy.Market.Code),
            "Staff user is not authorized for the claim's market.");
    }

    private void EnsureStaffCanPerform(StaffUser staffUser, StaffRole role)
    {
        EnsureStaffCanWorkInMarket(staffUser);
        DomainRule.Require(staffUser.CanPerform(role), $"Staff user must have the '{role}' role.");
    }

    private void EnsureStaffCanPerformAny(StaffUser staffUser, params StaffRole[] roles)
    {
        EnsureStaffCanWorkInMarket(staffUser);
        DomainRule.Require(
            roles.Any(staffUser.CanPerform),
            $"Staff user must have one of the following roles: {string.Join(", ", roles)}.");
    }

    private static StaffRole ToStaffRole(AssignmentRole role) =>
        role switch
        {
            AssignmentRole.ClaimsHandler => StaffRole.ClaimsHandler,
            AssignmentRole.Adjuster => StaffRole.Adjuster,
            AssignmentRole.Assessor => StaffRole.Assessor,
            AssignmentRole.Approver => StaffRole.Approver,
            _ => throw new DomainRuleViolationException("Assignment role is invalid.")
        };

    private static T FindEntity<T>(IEnumerable<T> entities, Guid id, string entityName)
        where T : Entity =>
        entities.FirstOrDefault(entity => entity.Id == id)
        ?? throw new DomainRuleViolationException($"{entityName} '{id}' was not found on this claim.");
}
