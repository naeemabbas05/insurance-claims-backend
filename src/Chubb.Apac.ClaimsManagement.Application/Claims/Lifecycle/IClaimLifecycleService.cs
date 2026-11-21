namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public interface IClaimLifecycleService
{
    Task<ClaimDecisionResult> GetDecisionAsync(
        string claimReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimInformationRequestItem>> GetInformationRequestsAsync(
        string claimReference,
        Guid claimantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimStatusHistoryItem>> GetStatusHistoryAsync(
        string claimReference,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimAuditHistoryItem>> GetAuditHistoryAsync(
        string claimReference,
        Guid staffUserId,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> RecordAssessmentAsync(
        RecordClaimAssessmentCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> ApproveAsync(
        ApproveClaimCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> RejectAsync(
        RejectClaimCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> RequestInformationAsync(
        RequestClaimInformationCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> RespondToInformationAsync(
        RespondToClaimInformationCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> CancelInformationRequestAsync(
        CancelClaimInformationCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> ProposeSettlementAsync(
        ProposeClaimSettlementCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> ApproveSettlementAsync(
        ApproveClaimSettlementCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> PaySettlementAsync(
        PayClaimSettlementCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> CancelSettlementAsync(
        CancelClaimSettlementCommand command,
        CancellationToken cancellationToken);

    Task<ClaimLifecycleResult> ChangeWorkflowStatusAsync(
        ChangeClaimWorkflowStatusCommand command,
        CancellationToken cancellationToken);
}
