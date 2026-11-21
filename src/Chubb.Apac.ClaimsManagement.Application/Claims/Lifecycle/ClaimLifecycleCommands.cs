using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public interface IStaffClaimCommand
{
    string ClaimReference { get; }

    Guid StaffUserId { get; }
}

public sealed record RecordClaimAssessmentCommand(
    string ClaimReference,
    Guid StaffUserId,
    decimal AssessedDamageAmount,
    decimal RecommendedSettlementAmount,
    string CurrencyCode,
    string Findings) : IStaffClaimCommand;

public sealed record ApproveClaimCommand(
    string ClaimReference,
    Guid StaffUserId,
    decimal ApprovedAmount,
    string CurrencyCode,
    string Reason) : IStaffClaimCommand;

public sealed record RejectClaimCommand(
    string ClaimReference,
    Guid StaffUserId,
    string Reason) : IStaffClaimCommand;

public sealed record RequestClaimInformationCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid ClaimantId,
    string Question,
    DateTimeOffset DueAtUtc) : IStaffClaimCommand;

public sealed record RespondToClaimInformationCommand(
    string ClaimReference,
    Guid ClaimantId,
    Guid RequestId,
    string Response);

public sealed record CancelClaimInformationCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid RequestId) : IStaffClaimCommand;

public sealed record ProposeClaimSettlementCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid PayeeClaimantId,
    decimal Amount,
    string CurrencyCode,
    bool IsFinal) : IStaffClaimCommand;

public sealed record ApproveClaimSettlementCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid SettlementId) : IStaffClaimCommand;

public sealed record PayClaimSettlementCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid SettlementId) : IStaffClaimCommand;

public sealed record CancelClaimSettlementCommand(
    string ClaimReference,
    Guid StaffUserId,
    Guid SettlementId) : IStaffClaimCommand;

public sealed record ChangeClaimWorkflowStatusCommand(
    string ClaimReference,
    Guid StaffUserId,
    ClaimStatus NewStatus,
    string Reason) : IStaffClaimCommand;
