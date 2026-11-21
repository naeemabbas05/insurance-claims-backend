using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ClaimLifecycleTests
{
    private static readonly DateTimeOffset Now = Utc(2026, 3, 12);

    [Fact]
    public async Task LifecycleValidatorsRequireRejectionReasonAndPositiveApprovalAmount()
    {
        var rejectValidation = await new RejectClaimCommandValidator().ValidateAsync(
            new RejectClaimCommand("CLM-MY-0123456789ABCDEF0123456789ABCDEF", Guid.NewGuid(), " "));
        var approvalValidation = await new ApproveClaimCommandValidator().ValidateAsync(
            new ApproveClaimCommand(
                "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                Guid.NewGuid(),
                0,
                "MYR",
                "Coverage confirmed."));

        Assert.False(rejectValidation.IsValid);
        Assert.Contains(rejectValidation.Errors, error => error.PropertyName == nameof(RejectClaimCommand.Reason));
        Assert.False(approvalValidation.IsValid);
        Assert.Contains(
            approvalValidation.Errors,
            error => error.PropertyName == nameof(ApproveClaimCommand.ApprovedAmount));
    }

    [Fact]
    public async Task LifecycleCanAssessApproveAndSettleClaimWithAuditHistory()
    {
        var claim = CreateClaim();
        var handler = CreateStaff(StaffRole.ClaimsHandler);
        var assessor = CreateStaff(StaffRole.Assessor);
        var approver = CreateStaff(StaffRole.Approver);
        var financeOfficer = CreateStaff(StaffRole.FinanceOfficer);
        var repository = new FakeClaimLifecycleRepository(claim, handler, assessor, approver, financeOfficer);
        var service = CreateService(repository);

        await service.ChangeWorkflowStatusAsync(
            new ChangeClaimWorkflowStatusCommand(
                claim.Number.Value,
                handler.Id,
                ClaimStatus.UnderReview,
                "Review started."),
            CancellationToken.None);
        await service.RecordAssessmentAsync(
            new RecordClaimAssessmentCommand(
                claim.Number.Value,
                assessor.Id,
                1000m,
                800m,
                "MYR",
                "Damage assessment completed."),
            CancellationToken.None);
        var approved = await service.ApproveAsync(
            new ApproveClaimCommand(
                claim.Number.Value,
                approver.Id,
                750m,
                "MYR",
                "Coverage confirmed."),
            CancellationToken.None);
        var settlement = await service.ProposeSettlementAsync(
            new ProposeClaimSettlementCommand(
                claim.Number.Value,
                handler.Id,
                claim.Claimants.Single().Id,
                750m,
                "MYR",
                IsFinal: true),
            CancellationToken.None);
        await service.ApproveSettlementAsync(
            new ApproveClaimSettlementCommand(claim.Number.Value, approver.Id, settlement.ResourceId!.Value),
            CancellationToken.None);
        var paid = await service.PaySettlementAsync(
            new PayClaimSettlementCommand(claim.Number.Value, financeOfficer.Id, settlement.ResourceId!.Value),
            CancellationToken.None);

        Assert.Equal(750m, approved.ApprovedAmount);
        Assert.Equal(ClaimStatus.Settled, paid.Status);
        Assert.Equal(6, repository.SaveCount);
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "AssessmentRecorded");
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "ClaimApproved");
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "SettlementPaid");

        var history = await service.GetStatusHistoryAsync(claim.Number.Value, CancellationToken.None);
        Assert.Equal(
            [ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.AssessmentInProgress,
                ClaimStatus.Approved, ClaimStatus.Settled],
            history.Select(item => item.NewStatus));
    }

    [Fact]
    public async Task InformationRequestResponseReturnsClaimToReviewAndIsAudited()
    {
        var claim = CreateClaim();
        var handler = CreateStaff(StaffRole.ClaimsHandler);
        var repository = new FakeClaimLifecycleRepository(claim, handler);
        var service = CreateService(repository);
        await service.ChangeWorkflowStatusAsync(
            new ChangeClaimWorkflowStatusCommand(
                claim.Number.Value,
                handler.Id,
                ClaimStatus.UnderReview,
                "Review started."),
            CancellationToken.None);
        var requested = await service.RequestInformationAsync(
            new RequestClaimInformationCommand(
                claim.Number.Value,
                handler.Id,
                claim.Claimants.Single().Id,
                "Please provide the repair invoice.",
                Now.AddDays(3)),
            CancellationToken.None);

        var responded = await service.RespondToInformationAsync(
            new RespondToClaimInformationCommand(
                claim.Number.Value,
                claim.Claimants.Single().Id,
                requested.ResourceId!.Value,
                "Invoice uploaded."),
            CancellationToken.None);

        Assert.Equal(ClaimStatus.AwaitingInformation, requested.Status);
        Assert.Equal(ClaimStatus.UnderReview, responded.Status);
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "InformationRequested");
        Assert.Contains(claim.AuditHistory, entry => entry.Action == "InformationResponseReceived");
    }

    private static ClaimLifecycleService CreateService(FakeClaimLifecycleRepository repository)
    {
        var clock = new FixedTimeProvider(Now);
        return new ClaimLifecycleService(
            repository,
            new RecordClaimAssessmentCommandValidator(),
            new ApproveClaimCommandValidator(),
            new RejectClaimCommandValidator(),
            new RequestClaimInformationCommandValidator(clock),
            new RespondToClaimInformationCommandValidator(),
            new CancelClaimInformationCommandValidator(),
            new ProposeClaimSettlementCommandValidator(),
            new ApproveClaimSettlementCommandValidator(),
            new PayClaimSettlementCommandValidator(),
            new CancelClaimSettlementCommandValidator(),
            new ChangeClaimWorkflowStatusCommandValidator(),
            clock);
    }

    private static Claim CreateClaim()
    {
        var marketCatalog = MarketCatalog.Default;
        var policy = Policy.Create(
            PolicyNumber.Create("POL-LIFECYCLE-1"),
            marketCatalog,
            MarketCode.Create("MY"),
            PolicyType.Motor,
            PersonName.Create("Avery Tan"),
            DateRange.Create(Utc(2025, 1, 1), Utc(2027, 1, 1)),
            Money.Create(50000m, "MYR"),
            Utc(2024, 12, 1));
        return Claim.Create(
            ClaimNumber.Create($"CLM-MY-{Guid.NewGuid():N}"),
            policy,
            ClaimType.Motor,
            Utc(2026, 3, 10),
            Utc(2026, 3, 11),
            "Vehicle damage following a road accident.",
            Money.Create(1200m, "MYR"),
            ClaimantDetails.Create(
                ClaimantRole.PolicyHolder,
                PersonName.Create("Avery Tan"),
                ContactDetails.Create("avery@example.com", "+60123456789")));
    }

    private static StaffUser CreateStaff(StaffRole role) =>
        StaffUser.Create(
            PersonName.Create($"{role} User"),
            EmailAddress.Create($"{role.ToString().ToLowerInvariant()}@example.com"),
            role,
            [MarketCode.Create("MY")],
            MarketCatalog.Default,
            Utc(2024, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeClaimLifecycleRepository(
        Claim claim,
        params StaffUser[] staffUsers) : IClaimLifecycleRepository
    {
        private readonly Dictionary<Guid, StaffUser> _staffUsers =
            staffUsers.ToDictionary(staff => staff.Id);

        public int SaveCount { get; private set; }

        public Task<Claim?> FindClaimAsync(
            ClaimNumber claimNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<Claim?>(claim.Number == claimNumber ? claim : null);

        public Task<StaffUser?> FindStaffUserAsync(
            Guid staffUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_staffUsers.GetValueOrDefault(staffUserId));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
