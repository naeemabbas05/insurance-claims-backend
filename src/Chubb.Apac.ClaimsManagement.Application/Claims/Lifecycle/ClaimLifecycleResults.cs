using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public sealed record ClaimLifecycleResult(
    string ClaimReference,
    ClaimStatus Status,
    decimal? ApprovedAmount,
    string? ApprovedCurrency,
    Guid? ResourceId);

public sealed record ClaimStatusHistoryItem(
    ClaimStatus? PreviousStatus,
    ClaimStatus NewStatus,
    DateTimeOffset ChangedAtUtc);

public sealed record ClaimDecisionResult(
    string ClaimNumber,
    ClaimStatus Status,
    ClaimDecisionDetails? Decision);

public sealed record ClaimDecisionDetails(
    ClaimStatus DecisionType,
    DateTimeOffset DecisionDate,
    decimal? ApprovedAmount,
    string? Currency,
    string Reason,
    string? SettlementStatus);

public sealed record ClaimInformationRequestItem(
    Guid RequestId,
    string Question,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset DueAtUtc,
    InformationRequestStatus Status,
    string? Response,
    DateTimeOffset? RespondedAtUtc);

public sealed record ClaimAuditHistoryItem(
    string Action,
    DateTimeOffset OccurredAtUtc,
    string? Details,
    Guid? StaffUserId,
    Guid? ClaimantId);
