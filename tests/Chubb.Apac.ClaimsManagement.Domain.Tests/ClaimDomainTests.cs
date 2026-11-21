using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Tests;

public sealed class ClaimDomainTests
{
    private static readonly DateTimeOffset PolicyStart = Utc(2025, 1, 1);
    private static readonly DateTimeOffset Incident = Utc(2026, 3, 10);
    private static readonly DateTimeOffset Reported = Utc(2026, 3, 11);

    [Fact]
    public void MarketCatalogSupportsSixValidatedConfigurableMarkets()
    {
        var defaults = MarketCatalog.Default;
        Assert.Equal(MarketCatalog.SupportedMarketCount, defaults.Markets.Count);
        Assert.Equal(
            ["MY", "ID", "SG", "KH", "TH", "AU"],
            defaults.Markets.Select(market => market.Code.Value));

        var configuredMarkets = defaults.Markets
            .Select(market => MarketDefinition.Create(
                market.Code.Value,
                $"{market.Name} claims market",
                market.DefaultCurrency.Value))
            .ToArray();
        var configuredCatalog = MarketCatalog.Create(configuredMarkets);

        Assert.Equal("Malaysia claims market", configuredCatalog.Get(MarketCode.Create("MY")).Name);
        Assert.Throws<DomainRuleViolationException>(() =>
            MarketCatalog.Create(configuredMarkets.Take(MarketCatalog.SupportedMarketCount - 1)));
        Assert.Throws<DomainRuleViolationException>(() =>
            MarketCatalog.Create(configuredMarkets
                .Skip(1)
                .Append(MarketDefinition.Create("SG", "Duplicate", "SGD"))));
    }

    [Fact]
    public void ClaimRequiresMatchingPolicyTypeAndCoveredIncidentTime()
    {
        var propertyPolicy = CreatePolicy(PolicyType.Property);

        Assert.Throws<DomainRuleViolationException>(() => CreateClaim(propertyPolicy, ClaimType.Motor));

        var expiredPolicy = CreatePolicy(
            PolicyType.Motor,
            DateRange.Create(Utc(2025, 1, 1), Utc(2026, 3, 10)));
        Assert.Throws<DomainRuleViolationException>(() => CreateClaim(expiredPolicy, ClaimType.Motor));

        var claim = CreateClaim(CreatePolicy(PolicyType.Motor), ClaimType.Motor);
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(ClaimType.Motor, claim.Type);
        Assert.Equal(claim.Id, claim.Claimants.Single().ClaimId);
        Assert.Equal(ClaimStatus.Submitted, claim.StatusHistory.Single().NewStatus);
    }

    [Fact]
    public void ApprovalAndSettlementEnforceAssessmentAuthorizationAndFinancialLimits()
    {
        var claim = CreateClaim(CreatePolicy(PolicyType.Motor), ClaimType.Motor);
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var assessor = CreateStaff(StaffRole.Assessor, "MY");
        var approver = CreateStaff(StaffRole.Approver, "MY");
        var financeOfficer = CreateStaff(StaffRole.FinanceOfficer, "MY");

        claim.ChangeStatus(ClaimStatus.UnderReview, handler, "Review started.", Reported.AddDays(1));
        Assert.Throws<DomainRuleViolationException>(() =>
            claim.Approve(approver, Money.Create(800m, "MYR"), "Approve.", Reported.AddDays(2)));

        claim.RecordAssessment(
            assessor,
            Money.Create(1000m, "MYR"),
            Money.Create(800m, "MYR"),
            "Repair estimate reviewed.",
            Reported.AddDays(3));
        Assert.Equal(ClaimStatus.AssessmentInProgress, claim.Status);

        Assert.Throws<DomainRuleViolationException>(() =>
            claim.Approve(approver, Money.Create(800.01m, "MYR"), "Amount exceeds recommendation.", Reported.AddDays(4)));
        Assert.Null(claim.ApprovedAmount);
        claim.Approve(approver, Money.Create(800m, "MYR"), "Coverage confirmed.", Reported.AddDays(4));
        Assert.Throws<DomainRuleViolationException>(() =>
            claim.ProposeSettlement(
                claim.Claimants.Single(),
                Money.Create(800.01m, "MYR"),
                isFinal: true,
                handler,
                Reported.AddDays(5)));

        var settlement = claim.ProposeSettlement(
            claim.Claimants.Single(),
            Money.Create(800m, "MYR"),
            isFinal: true,
            handler,
            Reported.AddDays(5));
        claim.ApproveSettlement(settlement.Id, approver, Reported.AddDays(6));
        claim.MarkSettlementPaid(settlement.Id, financeOfficer, Reported.AddDays(7));

        Assert.Equal(SettlementStatus.Paid, settlement.Status);
        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(ClaimStatus.Settled, claim.StatusHistory.Last().NewStatus);
        Assert.Equal(800m, claim.ApprovedAmount?.Amount);
        Assert.Throws<DomainRuleViolationException>(() =>
            claim.Reject(approver, "Attempt to reject settled claim.", Reported.AddDays(8)));
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "SettlementPaid");
    }

    [Fact]
    public void RejectionRequiresAReasonAndIsRecordedInAuditHistory()
    {
        var claim = CreateClaim(CreatePolicy(PolicyType.Motor), ClaimType.Motor);
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var approver = CreateStaff(StaffRole.Approver, "MY");
        var assessor = CreateStaff(StaffRole.Assessor, "MY");
        claim.ChangeStatus(ClaimStatus.UnderReview, handler, "Review started.", Reported.AddDays(1));

        Assert.Throws<DomainRuleViolationException>(() =>
            claim.Reject(approver, " ", Reported.AddDays(2)));
        Assert.Throws<DomainAuthorizationException>(() =>
            claim.Reject(assessor, "Not authorized.", Reported.AddDays(2)));

        claim.Reject(approver, "The reported loss is not covered.", Reported.AddDays(2));

        Assert.Equal(ClaimStatus.Declined, claim.Status);
        Assert.Equal("The reported loss is not covered.", claim.StatusHistory.Last().Reason);
        Assert.Contains(claim.AuditHistory, entry =>
            entry.Action == "ClaimRejected" && entry.Details == "The reported loss is not covered.");
    }

    [Fact]
    public void InformationRequestCanOnlyBeAnsweredByItsClaimant()
    {
        var claim = CreateClaim(CreatePolicy(PolicyType.Property), ClaimType.Property);
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var primaryClaimant = claim.Claimants.Single();
        var otherClaimant = claim.AddClaimant(
            ClaimantDetails.Create(
                ClaimantRole.Representative,
                PersonName.Create("Claims Representative"),
                ContactDetails.Create("representative@example.com", null)),
            Reported.AddMinutes(1));

        claim.ChangeStatus(ClaimStatus.UnderReview, handler, "Review started.", Reported.AddDays(1));
        var request = claim.RequestInformation(
            handler,
            primaryClaimant,
            "Please provide a repair invoice.",
            Reported.AddDays(2),
            Reported.AddDays(9));

        Assert.Equal(ClaimStatus.AwaitingInformation, claim.Status);
        Assert.Throws<DomainRuleViolationException>(() =>
            claim.RespondToInformationRequest(
                request.Id,
                otherClaimant,
                "This is not my request.",
                Reported.AddDays(3)));
        Assert.True(request.IsOverdueAt(Reported.AddDays(10)));

        claim.RespondToInformationRequest(
            request.Id,
            primaryClaimant,
            "Invoice attached.",
            Reported.AddDays(4));

        Assert.Equal(InformationRequestStatus.Responded, request.Status);
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
    }

    [Fact]
    public void StaffAssignmentsAreRestrictedToTheirConfiguredMarket()
    {
        var claim = CreateClaim(CreatePolicy(PolicyType.Motor), ClaimType.Motor);
        var indonesiaHandler = CreateStaff(StaffRole.ClaimsHandler, "ID");
        var malaysiaHandler = CreateStaff(StaffRole.ClaimsHandler, "MY");

        Assert.Throws<DomainAuthorizationException>(() =>
            claim.Assign(
                indonesiaHandler,
                AssignmentRole.ClaimsHandler,
                malaysiaHandler,
                Reported.AddDays(1)));
        Assert.Empty(claim.Assignments);
    }

    [Fact]
    public void ReassignmentPreservesHistoryAndRejectsConflictingActiveAssignments()
    {
        var claim = CreateClaim(CreatePolicy(PolicyType.Motor), ClaimType.Motor);
        var manager = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var firstAdjuster = CreateStaff(StaffRole.Adjuster, "MY");
        var secondAdjuster = CreateStaff(StaffRole.Adjuster, "MY");
        var firstAssignment = claim.Assign(
            firstAdjuster,
            AssignmentRole.Adjuster,
            manager,
            Reported.AddDays(1));

        Assert.Throws<AssignmentConflictException>(() =>
            claim.Assign(
                secondAdjuster,
                AssignmentRole.Adjuster,
                manager,
                Reported.AddDays(1)));

        var reassigned = claim.Reassign(
            firstAssignment.Id,
            secondAdjuster,
            manager,
            Reported.AddDays(2));

        Assert.False(firstAssignment.IsActive);
        Assert.Equal(Reported.AddDays(2), firstAssignment.UnassignedAtUtc);
        Assert.True(reassigned.IsActive);
        Assert.Equal(secondAdjuster.Id, reassigned.StaffUserId);
        Assert.Equal(3, claim.AssignmentVersion);
        Assert.Equal(2, claim.Assignments.Count);
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "StaffUnassigned");
        Assert.Equal(2, claim.AuditHistory.Count(entry => entry.Action == "StaffAssigned"));
    }

    private static Claim CreateClaim(Policy policy, ClaimType type) =>
        Claim.Create(
            ClaimNumber.Create($"CLM-{Guid.NewGuid():N}"),
            policy,
            type,
            Incident,
            Reported,
            "Accidental loss reported by the policy holder.",
            Money.Create(1200m, "MYR"),
            ClaimantDetails.Create(
                ClaimantRole.PolicyHolder,
                PersonName.Create("Avery Tan"),
                ContactDetails.Create("avery@example.com", "+60123456789")));

    private static Policy CreatePolicy(PolicyType type, DateRange? coveragePeriod = null) =>
        Policy.Create(
            PolicyNumber.Create($"POL-{Guid.NewGuid():N}"),
            MarketCatalog.Default,
            MarketCode.Create("MY"),
            type,
            PersonName.Create("Avery Tan"),
            coveragePeriod ?? DateRange.Create(PolicyStart, Utc(2027, 1, 1)),
            Money.Create(50000m, "MYR"),
            Utc(2024, 12, 1));

    private static StaffUser CreateStaff(StaffRole role, string market) =>
        StaffUser.Create(
            PersonName.Create($"{role} User"),
            EmailAddress.Create($"{role.ToString().ToLowerInvariant()}@example.com"),
            role,
            [MarketCode.Create(market)],
            MarketCatalog.Default,
            Utc(2024, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);
}
