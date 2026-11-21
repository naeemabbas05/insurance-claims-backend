using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public sealed class ClaimLifecycleService(
    IClaimLifecycleRepository repository,
    IValidator<RecordClaimAssessmentCommand> recordAssessmentValidator,
    IValidator<ApproveClaimCommand> approveValidator,
    IValidator<RejectClaimCommand> rejectValidator,
    IValidator<RequestClaimInformationCommand> requestInformationValidator,
    IValidator<RespondToClaimInformationCommand> respondToInformationValidator,
    IValidator<CancelClaimInformationCommand> cancelInformationValidator,
    IValidator<ProposeClaimSettlementCommand> proposeSettlementValidator,
    IValidator<ApproveClaimSettlementCommand> approveSettlementValidator,
    IValidator<PayClaimSettlementCommand> paySettlementValidator,
    IValidator<CancelClaimSettlementCommand> cancelSettlementValidator,
    IValidator<ChangeClaimWorkflowStatusCommand> changeStatusValidator,
    IClaimIntegrationEventOutbox eventOutbox,
    TimeProvider timeProvider) : IClaimLifecycleService
{
    public async Task<IReadOnlyList<ClaimStatusHistoryItem>> GetStatusHistoryAsync(
        string claimReference,
        CancellationToken cancellationToken)
    {
        var claim = await GetClaimAsync(claimReference, cancellationToken);
        return claim.StatusHistory
            .OrderBy(item => item.ChangedAtUtc)
            .Select(item => new ClaimStatusHistoryItem(
                item.PreviousStatus,
                item.NewStatus,
                item.ChangedAtUtc))
            .ToArray();
    }

    public async Task<IReadOnlyList<ClaimAuditHistoryItem>> GetAuditHistoryAsync(
        string claimReference,
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var claim = await GetClaimAsync(claimReference, cancellationToken);
        var staff = await GetStaffUserAsync(staffUserId, cancellationToken);
        if (!staff.CanWorkIn(claim.Policy.MarketCode))
        {
            throw new ClaimLifecycleAccessDeniedException();
        }

        return claim.AuditHistory
            .OrderBy(item => item.OccurredAtUtc)
            .Select(item => new ClaimAuditHistoryItem(
                item.Action,
                item.OccurredAtUtc,
                item.Details,
                item.StaffUserId,
                item.ClaimantId))
            .ToArray();
    }

    public async Task<ClaimLifecycleResult> RecordAssessmentAsync(
        RecordClaimAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        await recordAssessmentValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        var assessment = claim.RecordAssessment(
            staff,
            Money.Create(command.AssessedDamageAmount, command.CurrencyCode),
            Money.Create(command.RecommendedSettlementAmount, command.CurrencyCode),
            command.Findings,
            occurredAtUtc);
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim, assessment.Id);
    }

    public async Task<ClaimLifecycleResult> ApproveAsync(
        ApproveClaimCommand command,
        CancellationToken cancellationToken)
    {
        await approveValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.Approve(
            staff,
            Money.Create(command.ApprovedAmount, command.CurrencyCode),
            command.Reason,
            occurredAtUtc);
        eventOutbox.Enqueue(new ClaimApprovedV1(
            Guid.NewGuid(),
            occurredAtUtc,
            claim.Id,
            claim.Number.Value,
            claim.Policy.MarketCode.Value,
            staff.Id,
            claim.ApprovedAmount!.Amount,
            claim.ApprovedAmount.Currency.Value));
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim);
    }

    public async Task<ClaimLifecycleResult> RejectAsync(
        RejectClaimCommand command,
        CancellationToken cancellationToken)
    {
        await rejectValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.Reject(staff, command.Reason, occurredAtUtc);
        eventOutbox.Enqueue(new ClaimRejectedV1(
            Guid.NewGuid(),
            occurredAtUtc,
            claim.Id,
            claim.Number.Value,
            claim.Policy.MarketCode.Value,
            staff.Id));
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim);
    }

    public async Task<ClaimLifecycleResult> RequestInformationAsync(
        RequestClaimInformationCommand command,
        CancellationToken cancellationToken)
    {
        await requestInformationValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var claimant = claim.Claimants.FirstOrDefault(item => item.Id == command.ClaimantId)
            ?? throw new DomainRuleViolationException("Claimant does not belong to this claim.");
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        var request = claim.RequestInformation(
            staff,
            claimant,
            command.Question,
            occurredAtUtc,
            command.DueAtUtc);
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim, request.Id);
    }

    public async Task<ClaimLifecycleResult> RespondToInformationAsync(
        RespondToClaimInformationCommand command,
        CancellationToken cancellationToken)
    {
        await respondToInformationValidator.ValidateAndThrowAsync(command, cancellationToken);
        var claim = await GetClaimAsync(command.ClaimReference, cancellationToken);
        var claimant = claim.Claimants.FirstOrDefault(item => item.Id == command.ClaimantId)
            ?? throw new ClaimLifecycleAccessDeniedException();
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.RespondToInformationRequest(
            command.RequestId,
            claimant,
            command.Response,
            occurredAtUtc);
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            claimant.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim, command.RequestId);
    }

    public async Task<ClaimLifecycleResult> CancelInformationRequestAsync(
        CancelClaimInformationCommand command,
        CancellationToken cancellationToken)
    {
        await cancelInformationValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.CancelInformationRequest(command.RequestId, staff, occurredAtUtc);
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim, command.RequestId);
    }

    public async Task<ClaimLifecycleResult> ProposeSettlementAsync(
        ProposeClaimSettlementCommand command,
        CancellationToken cancellationToken)
    {
        await proposeSettlementValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var payee = claim.Claimants.FirstOrDefault(item => item.Id == command.PayeeClaimantId)
            ?? throw new DomainRuleViolationException("Settlement payee does not belong to this claim.");
        var settlement = claim.ProposeSettlement(
            payee,
            Money.Create(command.Amount, command.CurrencyCode),
            command.IsFinal,
            staff,
            timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return ToResult(claim, settlement.Id);
    }

    public async Task<ClaimLifecycleResult> ApproveSettlementAsync(
        ApproveClaimSettlementCommand command,
        CancellationToken cancellationToken)
    {
        await approveSettlementValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        claim.ApproveSettlement(command.SettlementId, staff, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return ToResult(claim, command.SettlementId);
    }

    public async Task<ClaimLifecycleResult> PaySettlementAsync(
        PayClaimSettlementCommand command,
        CancellationToken cancellationToken)
    {
        await paySettlementValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.MarkSettlementPaid(command.SettlementId, staff, occurredAtUtc);
        if (previousStatus != ClaimStatus.Settled && claim.Status == ClaimStatus.Settled)
        {
            var paidSettlements = claim.Settlements
                .Where(settlement => settlement.Status == SettlementStatus.Paid)
                .ToArray();
            eventOutbox.Enqueue(new ClaimSettledV1(
                Guid.NewGuid(),
                occurredAtUtc,
                claim.Id,
                claim.Number.Value,
                claim.Policy.MarketCode.Value,
                staff.Id,
                paidSettlements.Sum(settlement => settlement.Amount.Amount),
                paidSettlements[0].Amount.Currency.Value));
        }

        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim, command.SettlementId);
    }

    public async Task<ClaimLifecycleResult> CancelSettlementAsync(
        CancelClaimSettlementCommand command,
        CancellationToken cancellationToken)
    {
        await cancelSettlementValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        claim.CancelSettlement(command.SettlementId, staff, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return ToResult(claim, command.SettlementId);
    }

    public async Task<ClaimLifecycleResult> ChangeWorkflowStatusAsync(
        ChangeClaimWorkflowStatusCommand command,
        CancellationToken cancellationToken)
    {
        await changeStatusValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, staff) = await GetClaimAndStaffAsync(
            command.ClaimReference,
            command.StaffUserId,
            cancellationToken);
        var previousStatus = claim.Status;
        var occurredAtUtc = timeProvider.GetUtcNow();
        claim.ChangeStatus(
            command.NewStatus,
            staff,
            command.Reason,
            occurredAtUtc);
        await SaveStatusChangeAsync(
            claim,
            previousStatus,
            staff.Id,
            occurredAtUtc,
            cancellationToken);
        return ToResult(claim);
    }

    private async Task<Claim> GetClaimAsync(
        string claimReference,
        CancellationToken cancellationToken)
    {
        if (!IsValidClaimReference(claimReference))
        {
            throw new ClaimLifecycleNotFoundException(claimReference);
        }

        var claimNumber = ClaimNumber.Create(claimReference);
        return await repository.FindClaimAsync(claimNumber, cancellationToken)
            ?? throw new ClaimLifecycleNotFoundException(claimNumber.Value);
    }

    private async Task<(Claim Claim, StaffUser Staff)> GetClaimAndStaffAsync(
        string claimReference,
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var claim = await GetClaimAsync(claimReference, cancellationToken);
        var staff = await GetStaffUserAsync(staffUserId, cancellationToken);
        if (!staff.CanWorkIn(claim.Policy.MarketCode))
        {
            throw new ClaimLifecycleAccessDeniedException();
        }

        return (claim, staff);
    }

    private async Task<StaffUser> GetStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var staff = await repository.FindStaffUserAsync(staffUserId, cancellationToken);
        return staff is { IsActive: true }
            ? staff
            : throw new ClaimLifecycleActorNotFoundException(staffUserId);
    }

    private static ClaimLifecycleResult ToResult(Claim claim, Guid? resourceId = null) =>
        new(
            claim.Number.Value,
            claim.Status,
            claim.ApprovedAmount?.Amount,
            claim.ApprovedAmount?.Currency.Value,
            resourceId);

    private async Task SaveStatusChangeAsync(
        Claim claim,
        ClaimStatus previousStatus,
        Guid? actorId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (claim.Status != previousStatus)
        {
            eventOutbox.Enqueue(new ClaimStatusChangedV1(
                Guid.NewGuid(),
                occurredAtUtc,
                claim.Id,
                claim.Number.Value,
                claim.Policy.MarketCode.Value,
                previousStatus.ToString(),
                claim.Status.ToString(),
                actorId));
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private static bool IsValidClaimReference(string? value) =>
        value is { Length: 39 } &&
        value.StartsWith("CLM-", StringComparison.OrdinalIgnoreCase) &&
        value[4..6].All(char.IsAsciiLetter) &&
        value[6] == '-' &&
        value[7..].All(Uri.IsHexDigit);
}
