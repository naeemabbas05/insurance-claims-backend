using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Api.Models;

public sealed record RecordClaimAssessmentRequest(
    decimal AssessedDamageAmount,
    decimal RecommendedSettlementAmount,
    string? CurrencyCode,
    string? Findings);

public sealed record ApproveClaimRequest(
    decimal ApprovedAmount,
    string? CurrencyCode,
    string? Reason);

public sealed record RejectClaimRequest(string? Reason);

public sealed record RequestClaimInformationRequest(
    Guid ClaimantId,
    string? Question,
    DateTimeOffset DueAtUtc);

public sealed record RespondToClaimInformationRequest(string? Response);

public sealed record ProposeClaimSettlementRequest(
    Guid PayeeClaimantId,
    decimal Amount,
    string? CurrencyCode,
    bool IsFinal);

public sealed record ChangeClaimWorkflowStatusRequest(
    ClaimStatus NewStatus,
    string? Reason);
