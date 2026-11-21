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

public sealed record ClaimAuditHistoryItem(
    string Action,
    DateTimeOffset OccurredAtUtc,
    string? Details,
    Guid? StaffUserId,
    Guid? ClaimantId);
